using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FivePD.API;
using FivePD.API.Utils;
using MenuAPI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FivePD_DrugTestKit_Jeremiah;

public class DrugTest : Plugin
{
    public static List<Item> collectedItems = new List<Item>();
    public bool isPlayerInTrafficStop = false;
    public bool isPlayerStoppingPed = false;
    private Vehicle lastSearchedVehicle = null;
    private List<Entity> stoppedPeds = new List<Entity>();
    private Ped lastSearchedPed = null;
    public static bool _x = false; // INPUT_VEH_DUCK
    public static bool _down1 = false; // INPUT_FRONTEND_DOWN
    public static bool _down2 = false; // 
    public static bool _down3 = false; //
    public static bool _down4 = false; // [Meaning] Selected search
    public static bool _right = false; // [Condition] Down4 == true [Meaning] Vehicle Search 
    public static bool _down5 = false;
    public static bool _down6 = false;
    public static bool _down7 = false;
    public bool duty = false;
    public static JArray itemsJSON;

    internal DrugTest()
    {
        // Startup
        //Debug.WriteLine("Loaded DrugTest 1.0 by DevKilo");
        EventHandlers["FIVEPD::Client::changePedState"] += changePedState;
        // Post-Startup
        Events.OnDutyStatusChange += EventsOnOnDutyStatusChange;
        CheckForSearch();
    }

    private Task EventsOnOnDutyStatusChange(bool onduty)
    {
        duty = onduty;
        return Task.FromResult(0);
    }

    private bool IsItemSuspicious(string itemName)
    {
        bool result = false;
        foreach (JToken obj in itemsJSON)
        {
            if (obj["isSuspicious"] == null) continue;
            //Debug.WriteLine(obj["isSuspicious"].ToString());
            //Debug.WriteLine(obj.ToString());
            if (obj["name"].ToString() == itemName)
            {
                result = bool.Parse(obj["isSuspicious"].ToString());
                break;
            }
        }

        return result;
    }

    public static JArray JSONRead(string fileName)
    {
        string data =
            API.LoadResourceFile(API.GetCurrentResourceName(), fileName); // !!Add to files in fxmanifest.lua!!
        //Debug.WriteLine("Data: "+data);
        return JArray.Parse(data);
    }

    private void changePedState(string datax)
    {
        //Debug.WriteLine(datax);
        JObject data = JObject.Parse(datax);

        int netId = (int)data?["networkId"];
        bool isStopped = false;
        bool remove = false;
        if (data["isStopped"] != null)
        {
            isStopped = (bool)data?["isStopped"];
        }

        if (data["remove"] != null)
        {
            remove = (bool)data?["remove"];
        }

        if (remove)
        {
            //Debug.WriteLine("Cancelled stopped ped");
            isPlayerStoppingPed = false;
            stoppedPeds.Remove((Ped)Entity.FromNetworkId(netId));
        }

        if (isStopped && !isPlayerInTrafficStop)
        {
            if (Utilities.IsPlayerPerformingTrafficStop()) isPlayerInTrafficStop = true;
            //Debug.WriteLine("Player is now in traffic stop with ped");
            isPlayerStoppingPed = true;
            if (!stoppedPeds.Contains((Ped)Entity.FromNetworkId(netId)))
                stoppedPeds.Add((Ped)Entity.FromNetworkId(netId));
        }
        else if (!isStopped && isPlayerInTrafficStop)
        {
            if (!Utilities.IsPlayerPerformingTrafficStop()) isPlayerInTrafficStop = false;
            //Debug.WriteLine("Player is no longer in traffic stop with ped");
            isPlayerStoppingPed = false;
            stoppedPeds.Remove((Ped)Entity.FromNetworkId(netId));
        }

        if (stoppedPeds.Count < 1)
        {
            ClearItems();
        }
        else
        {
            foreach (var p in stoppedPeds)
            {
                //Debug.WriteLine("Ped in stoppedPeds: "+p.Handle.ToString());
            }
        }
    }

        JObject LangConfig;
    private async void CheckForSearch()
    {
        LangConfig = JObject.Parse(API.LoadResourceFile("fivepd", "languages/en.json") ?? "{}") ?? new JObject();
        
        
        var searchButtonName = "Search";
        if (LangConfig.TryGetValue("search", out var searchName))
            searchButtonName = (string)searchName;
        
        var pedStopMenuName = "Ped stop menu";

        if (LangConfig.TryGetValue("PedCheckMenu", out var pedStop))
        {
            pedStopMenuName = (string)pedStop["title"];
        }

        var pedStopmenu = MenuController.Menus.FirstOrDefault(menu => menu.MenuTitle == pedStopMenuName);
        if (pedStopmenu is null)
        {
            Debug.WriteLine("Failed to get Ped stop menu!");
            return;
        }
        
        MenuListItem searchButton = (MenuListItem)pedStopmenu.GetMenuItems().FirstOrDefault(i => i.Text == searchButtonName);
        searchButton.ParentMenu.OnListItemSelect += async (menu, item, index, itemIndex) =>
        {
            if (item == searchButton)
            {
                var searchType = searchButton.ListItems[searchButton.ListIndex];
                OnSearchButton(searchType);
            }
        };

        pedStopmenu.OnItemSelect += async (menu, item, index) =>
        {
            
        };
        
        
        /*// Down 4
        Tick += async () =>
        {
            // Control Enable
            if (!_x && duty && Game.IsControlJustPressed(0, Control.VehicleDuck) && !Game.PlayerPed.IsInVehicle() &&
                (World.GetAllPeds().FirstOrDefault(p =>
                    p != null && p.Exists() && p.NetworkId != Game.PlayerPed.NetworkId &&
                    p.Position.DistanceTo(Game.PlayerPed.Position) < 3f && !p.IsInVehicle() &&
                    stoppedPeds.Contains(p))) != null)
            {
                Debug.WriteLine("X Menu is now open");
                _x = true;
            }
            else if (_x && !_down1 && Game.IsControlJustPressed(0, Control.FrontendDown) ||
                     _x && !_down1 && Game.IsControlJustPressed(0, Control.WeaponWheelNext))
            {
                Debug.WriteLine("Down 1");
                _down1 = true;
            }
            else if (_down1 && !_down2 && Game.IsControlJustPressed(0, Control.FrontendDown) ||
                     _down1 && !_down2 && Game.IsControlJustPressed(0, Control.WeaponWheelNext))
            {
                Debug.WriteLine("Down 2");
                _down2 = true;
            }
            else if (_down2 && !_down3 && Game.IsControlJustPressed(0, Control.FrontendDown) ||
                     _down2 && !_down3 && Game.IsControlJustPressed(0, Control.WeaponWheelNext))
            {
                Debug.WriteLine("Down 3");
                _down3 = true;
            }
            else if (_down3 && !_down4 && Game.IsControlJustPressed(0, Control.FrontendDown) ||
                     _down3 && !_down4 && Game.IsControlJustPressed(0, Control.WeaponWheelNext))
            {
                Debug.WriteLine("Down 4");
                _down4 = true;
            }
            else if (_down4 && !_down5 && !_right && Game.IsControlJustPressed(0, Control.FrontendRight))
            {
                Debug.WriteLine("Right");
                _right = true;
            }
            else if (_down4 && !_down5 && Game.IsControlJustPressed(0, Control.FrontendDown))
            {
                Debug.WriteLine("Down 5");
                _down5 = true;
            }
            else if (_down5 && !_down6 && Game.IsControlJustPressed(0, Control.FrontendDown))
            {
                Debug.WriteLine("Down 6");
                _down6 = true;
            }
            else if (_down6 && !_down7 && Game.IsControlJustPressed(0, Control.FrontendDown))
            {
                Debug.WriteLine("Down 7");
                _down7 = true;
            }
            else if (_down7 && Game.IsControlJustPressed(0, Control.FrontendDown))
            {
                Debug.WriteLine("Back to top");
                _down7 = false;
                _down6 = false;
                _down5 = false;
                _down4 = false;
                _down3 = false;
                _down2 = false;
                _down1 = false;
            }
            else if (!_down1 && Game.IsControlJustPressed(0, Control.FrontendUp))
            {
                _down1 = true;
                _down2 = true;
                _down3 = true;
                _down4 = true;
                _down5 = true;
                _down6 = true;
                _down7 = true;
                Debug.WriteLine("Back to bottom");
            }
            // Control Disable
            else if (_x && Game.IsControlJustPressed(0, Control.FrontendCancel))
            {
                Debug.WriteLine("Menu close");
                _x = false;
            }
            else if (_x && Game.IsControlJustPressed(0, Control.VehicleDuck) /* && !Game.PlayerPed.IsInVehicle())
            {
                _x = false;
            }
            else if (Game.IsControlJustPressed(0, Control.FrontendUp) ||
                     Game.IsControlJustPressed(0, Control.WeaponWheelPrev))
            {
                if (_x)
                {
                    if (_down1 && _down2 && _down3 && _down4 && _down5 && _down6 && _down7)
                    {
                        _down7 = false;
                        Debug.WriteLine("Up to down6");
                    }

                    else if (_down1 && _down2 && _down3 && _down4 && _down5 && _down6 && !_down7)
                    {
                        _down6 = false;
                        Debug.WriteLine("Up to down5");
                    }

                    else if (_down1 && _down2 && _down3 && _down4 && _down5 && !_down6 && !_down7)
                    {
                        _down5 = false;
                        Debug.WriteLine("Up to down4");
                    }

                    else if (_down1 && _down2 && _down3 && _down4)
                    {
                        _down4 = false;
                        Debug.WriteLine("Up to down3");
                    }

                    else if (_down1 && _down2 && _down3 && !_down4)
                    {
                        Debug.WriteLine("Up to down2");
                        _down3 = false;
                    }

                    else if (_down1 && _down2 && !_down3)
                    {
                        Debug.WriteLine("Up to down");
                        _down2 = false;
                    }

                    else if (_down1 && !_down2)
                    {
                        Debug.WriteLine("Undo down1");
                        _down1 = false;
                    }
                }
            }
            else if (Game.IsControlJustPressed(0, Control.FrontendLeft))
            {
                if (_right && _down4)
                {
                    Debug.WriteLine("Back to ped search");
                    _right = false;
                }
            } /*else if (_down7 && Game.IsControlJustPressed(0, Control.SkipCutscene))
            {
                _x = false;
            }
            // Enter

            else if (_x && _down4 && !_down5 && Game.IsControlJustPressed(0, Control.SkipCutscene))
            {
                if (!isPlayerInTrafficStop && !isPlayerStoppingPed)
                {
                    return;
                }

                Ped closestPed = await GetClosestStoppedPed();
                if (closestPed.IsInVehicle()) return;

                if (!_right) // Ped Search Initiates
                {
                    //Debug.WriteLine("Ped Search");
                    Ped ped = null;
                    if (isPlayerInTrafficStop)
                    {
                        ped = Utilities.GetDriverFromTrafficStop();
                    }
                    else if (isPlayerStoppingPed)
                    {
                        ped = await GetClosestStoppedPed();
                    }

                    PedData data = await ped.GetData();
                    //Debug.WriteLine("After getdata");
                    List<Item> items = data.Items;
                    foreach (var item in items)
                    {
                        string name = item.Name;
                        // Grab items.json and find entry for [name]
                        // Check if isSuspicious and get drugReagent
                        bool isSuspicious = IsItemSuspicious(name); // Change to value in items.json
                        //Debug.WriteLine(name+" || isSuspicious: "+isSuspicious.ToString());
                        if (isSuspicious)
                        {
                            collectedItems.Add(item);
                        }
                    }

                    foreach (Item item in collectedItems)
                    {
                        MenuItem i = new MenuItem("~y~" + item.Name);
                        DrugMenu._menu.AddMenuItem(i);
                        MenuController.BindMenuItem(DrugMenu._menu, DrugMenu.testkits, i);
                        i.ItemData = item.Name;
                    }

                    lastSearchedPed = ped;
                }
                else // Vehicle Search Initiates
                {
                    //Debug.WriteLine("Vehicle search");
                    Vehicle vehicle;
                    if (Utilities.IsPlayerPerformingTrafficStop())
                        vehicle = Utilities.GetVehicleFromTrafficStop();
                    else
                        vehicle = await GetClosestVehicle();
                    VehicleData data = await vehicle.GetData();
                    List<Item> items = data.Items;
                    foreach (var item in items)
                    {
                        string name = item.Name;
                        // Grab items.json and find entry for [name]
                        // Check if isSuspicious and get drugReagent
                        bool isSuspicious = IsItemSuspicious(name); // Change to value in items.json
                        //Debug.WriteLine(name+" || isSuspicious: "+isSuspicious.ToString());
                        if (isSuspicious)
                        {
                            collectedItems.Add(item);
                        }
                    }

                    foreach (Item item in collectedItems)
                    {
                        MenuItem i = new MenuItem("~y~" + item.Name);
                        if (DrugMenu._menu.GetMenuItems().Contains(i)) continue;
                        DrugMenu._menu.AddMenuItem(i);
                        MenuController.BindMenuItem(DrugMenu._menu, DrugMenu.testkits, i);
                        i.ItemData = item.Name;
                    }

                    lastSearchedVehicle = vehicle;
                }
            }


            //
        };*/
        await Delay(5000);
        itemsJSON = JSONRead("/config/items.json");
    }

    private async void OnSearchButton(string searchType)
    {
        var searchTypes = new JArray() { "Ped", "Vehicle" };

        if (LangConfig is not null)
        {
            if (LangConfig.TryGetValue("PedCheckMenu", out var pedStopMenu))
            {
                if (((JObject)pedStopMenu).TryGetValue("search", out var search))
                {
                    if (((JObject)search).TryGetValue("types", out var _types))
                    {
                        searchTypes = (JArray)_types;
                    }
                }
            }
        }
        
        if (searchType == (string)searchTypes[0]) // Ped Search Initiates
        {
            Ped closestPed = await GetClosestStoppedPed();
            if (closestPed.IsInVehicle()) return;
            //Debug.WriteLine("Ped Search");
            Ped ped = null;
            if (isPlayerInTrafficStop)
            {
                ped = Utilities.GetDriverFromTrafficStop();
            }
            else if (isPlayerStoppingPed)
            {
                ped = await GetClosestStoppedPed();
            }

            PedData data = await ped.GetData();
            //Debug.WriteLine("After getdata");
            List<Item> items = data.Items;
            foreach (var item in items)
            {
                string name = item.Name;
                // Grab items.json and find entry for [name]
                // Check if isSuspicious and get drugReagent
                bool isSuspicious = IsItemSuspicious(name); // Change to value in items.json
                //Debug.WriteLine(name+" || isSuspicious: "+isSuspicious.ToString());
                if (isSuspicious)
                {
                    collectedItems.Add(item);
                }
            }

            foreach (Item item in collectedItems)
            {
                MenuItem i = new MenuItem("~y~" + item.Name);
                DrugMenu._menu.AddMenuItem(i);
                MenuController.BindMenuItem(DrugMenu._menu, DrugMenu.testkits, i);
                i.ItemData = item.Name;
            }

            lastSearchedPed = ped;
        }
        else // Vehicle Search Initiates
        {
            //Debug.WriteLine("Vehicle search");
            Vehicle vehicle;
            if (Utilities.IsPlayerPerformingTrafficStop())
                vehicle = Utilities.GetVehicleFromTrafficStop();
            else
                vehicle = await GetClosestVehicle();
            VehicleData data = await vehicle.GetData();
            List<Item> items = data.Items;
            foreach (var item in items)
            {
                string name = item.Name;
                // Grab items.json and find entry for [name]
                // Check if isSuspicious and get drugReagent
                bool isSuspicious = IsItemSuspicious(name); // Change to value in items.json
                //Debug.WriteLine(name+" || isSuspicious: "+isSuspicious.ToString());
                if (isSuspicious)
                {
                    collectedItems.Add(item);
                }
            }

            foreach (Item item in collectedItems)
            {
                MenuItem i = new MenuItem("~y~" + item.Name);
                if (DrugMenu._menu.GetMenuItems().Contains(i)) continue;
                DrugMenu._menu.AddMenuItem(i);
                MenuController.BindMenuItem(DrugMenu._menu, DrugMenu.testkits, i);
                i.ItemData = item.Name;
            }

            lastSearchedVehicle = vehicle;
        }
    }


    private async Task<Ped> GetClosestStoppedPed()
    {
        Ped closestPed = null;
        foreach (Ped p in stoppedPeds)
        {
            if (closestPed == null)
            {
                closestPed = p;
            }

            if (p.Position.DistanceTo(Game.PlayerPed.Position) <
                closestPed.Position.DistanceTo(Game.PlayerPed.Position))
            {
                closestPed = p;
            }
        }

        return closestPed;
    }

    private async Task<Vehicle> GetClosestVehicle()
    {
        Vehicle closestVehicle = null;
        foreach (Vehicle veh in World.GetAllVehicles())
        {
            if (closestVehicle == null)
            {
                closestVehicle = veh;
            }

            if (veh.Position.DistanceTo(Game.PlayerPed.Position) <
                closestVehicle.Position.DistanceTo(Game.PlayerPed.Position))
                closestVehicle = veh;
        }

        return closestVehicle;
    }

    private void ClearItems()
    {
        collectedItems.Clear();
        DrugMenu._menu.ClearMenuItems();
        //Debug.WriteLine("Should have removed all items");
    }
}

public class DrugMenu : BaseScript
{
    public static Menu _menu;
    public static Menu testkits;
    private MenuItem currentItem;

    public DrugMenu()
    {
        if (!API.HasAnimDictLoaded("mini@repair"))
            API.RequestAnimDict("mini@repair");
        if (!API.HasAnimSetLoaded("fixing_a_player"))
            API.RequestAnimSet("fixing_a_player");
        API.RegisterCommand("menuresetnft", new Action<int, List<object>, string>((source, args, rawCommand) =>
        {
            DrugTest._x = false;
            DrugTest._down1 = false;
            DrugTest._down2 = false;
            DrugTest._down3 = false;
            DrugTest._down4 = false;
            DrugTest._down5 = false;
            DrugTest._down6 = false;
            DrugTest._down7 = false;
            DrugTest._right = false;
        }), false);
        TriggerEvent("chat:addSuggestion", "/menuresetnft",
            "~r~Set search back to 'Ped' and go to the top of the menu before triggering this command");

        _menu = new Menu("Narcotics Field Test", "~b~Select Suspicious Evidence~s~");
        MenuController.AddMenu(_menu);
        API.RegisterCommand("/-openNFTMenu", new Action<int, List<object>, string>((source, args, rawCommand) =>
        {
            Vehicle[] allVehicles = World.GetAllVehicles();
            Vehicle veh = null;
            foreach (var vehicle in allVehicles)
            {
                if (veh == null) veh = vehicle;

                if (Game.PlayerPed.Position.DistanceTo(vehicle.Position) <
                    veh.Position.DistanceTo(Game.PlayerPed.Position))
                {
                    veh = vehicle;
                }
            }

            var trunk = API.GetEntityBoneIndexByName(veh.Handle, "boot");
            if (veh.ClassDisplayName.ToString() == "VEH_CLASS_18")
            {
                if (veh.Doors[VehicleDoorIndex.Trunk].IsOpen)
                {
                    Vector3 coords = API.GetWorldPositionOfEntityBone(veh.Handle, trunk);
                    if (Game.PlayerPed.Position.DistanceTo(coords) < 1.5f)
                    {
                        _menu.Visible = !_menu.Visible;
                        LoopUntilPlayerIsOutOfRange(veh);
                    }
                }
            }
        }), false);
        API.RegisterKeyMapping("/-openNFTMenu", "Narcotics Drug System by DevKilo", "keyboard", "h");
        testkits = new Menu("Narcotics Field Test", "~b~Select Test Kit~s~");
        // Reagents
        MenuItem MJReagent = new MenuItem("Duquenois-Levine Reagent", "~b~Test Kit for~s~ ~y~Marijuana~s~ (powder)");
        MenuItem CocaineReagent = new MenuItem("Scott Reagent", "~b~Test Kit for~s~ ~y~Cocaine~s~ (powder, crystal)");
        MenuItem HeroinReagent = new MenuItem("Mecke Reagent", "~b~Test Kit for~s~ ~y~Heroin~s~ (powder)");
        MenuItem MethReagent =
            new MenuItem("Mandelin Reagent", "~b~Test Kit for~s~ ~y~Methamphetamine~s~ (powder, crystal)");
        MenuItem EcstasyReagent = new MenuItem("Mollies Reagent",
            "~b~Test Kit for~s~ ~y~Ecstacy/MDMA~s~ (pill, tablet, capsule)");
        MenuItem LSDReagent = new MenuItem("Ehrlich Reagent",
            "~b~Test Kit for~s~ ~y~LSD~s~ (pill, tablet, capsule, crystal, liquid, blotter paper)");
        MenuItem FentanylReagent = new MenuItem("Fentanyl Reagent",
            "~b~Test Kit for~s~ ~y~Fentanyl~s~ (pill, tablet, capsule, liquid, powder, blotter paper)");
        MenuItem PCPReagent = new MenuItem("PCP Reagent",
            "~b~Test Kit for~s~ ~y~PCP~s~ (pill, tablet, capsule, powder, crystal, liquid)");
        /*
         * Duquenois-Levine Reagent, ~b~Test Kit for~s~ ~y~Marijuana~s~ (Powder)
         * "Scott Reagent", "~b~Test Kit for~s~ ~y~Cocaine~s~ (powder, crystal)"
         * "Mecke Reagent", "~b~Test Kit for~s~ ~y~Heroin~s~ (powder)"
         * "Mandelin Reagent", "~b~Test Kit for~s~ ~y~Methamphetamine~s~ (powder, crystal)"
         * "Mollies Reagent", "~b~Test Kit for~s~ ~y~Ecstacy/MDMA~s~ (pill, tablet, capsule)"
         * "Ehrlich Reagent", "~b~Test Kit for~s~ ~y~LSD~s~ (pill, tablet, capsule, crystal, liquid, blotter paper)"
         * "Fentanyl Reagent", "~b~Test Kit for~s~ ~y~Fentanyl~s~ (pill, tablet, capsule, liquid, powder, blotter paper)"
         * "PCP Reagent", "~b~Test Kit for~s~ ~y~PCP~s~ (pill, tablet, capsule, powder, crystal, liquid)"
         */
        testkits.AddMenuItem(MJReagent);
        MJReagent.ItemData = "Marijuana";
        testkits.AddMenuItem(CocaineReagent);
        CocaineReagent.ItemData = "Cocaine";
        testkits.AddMenuItem(HeroinReagent);
        HeroinReagent.ItemData = "Heroin";
        testkits.AddMenuItem(MethReagent);
        MethReagent.ItemData = "Methamphetamines";
        testkits.AddMenuItem(EcstasyReagent);
        EcstasyReagent.ItemData = "Ecstasy/MDMA";
        testkits.AddMenuItem(LSDReagent);
        LSDReagent.ItemData = "LSD";
        testkits.AddMenuItem(FentanylReagent);
        FentanylReagent.ItemData = "Fentanyl";
        testkits.AddMenuItem(PCPReagent);
        PCPReagent.ItemData = "PCP";
        //
        _menu.OnItemSelect += OnMenuItemSelect;
        testkits.OnItemSelect += OnMenuItemSelect;
    }

    private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int index)
    {
        //Debug.WriteLine(menu.MenuTitle.ToString());
        if (menu == _menu)
        {
            if (currentItem != menuItem)
            {
                currentItem = menuItem;
                foreach (MenuItem item in testkits.GetMenuItems())
                {
                    item.Enabled = true;
                }
            }
        }

        if (menu == testkits)
        {
            string itemName = currentItem.ItemData;
            string drugType = GetDrugType(itemName);
            string reagent = GetDrugReagent(drugType);
            menuItem.Enabled = false;
            //Debug.WriteLine("Reagent: "+reagent);
            API.TaskPlayAnim(Game.PlayerPed.Handle, "mini@repair",
                "fixing_a_player", 4f, 4f, 5000, 1, 1f, false, false, false);
            //API.TaskStartScenarioAtPosition(Game.PlayerPed.Handle,"PROP_HUMAN_BUM_BIN",Game.PlayerPed.Position.X,Game.PlayerPed.Position.Y,Game.PlayerPed.Position.Z,Game.PlayerPed.Heading,1000,false,false);
            await Delay(3000);
            ShowTestResults(itemName, reagent, menuItem.Text, menuItem.ItemData);
        }
    }

    private void ShowTestResults(string itemName, string targetReagent, string reagent, string drugType)
    {
        string result = "~g~NEGATIVE";
        if (reagent == targetReagent) result = "~r~POSITIVE";
        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
            "~s~Item: ~y~" + itemName + "\n~s~Tested for: ~o~" + drugType + "\n~s~Result: " + result);
        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT, "commonmenu", "mp_specitem_weed", false, 0,
            "Narcotics Field Test", "~f~" + reagent);
        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
    }

    public string GetDrugReagent(string drugType)
    {
        string result = "Undefined";

        foreach (var menuItem in testkits.GetMenuItems())
        {
            if (menuItem.ItemData == drugType)
            {
                result = menuItem.Text;
                break;
            }
        }

        return result;
    }

    public string GetDrugType(string itemName)
    {
        string result = "Undefined";
        foreach (var obj in DrugTest.itemsJSON)
        {
            if ((string)obj["name"] == itemName)
            {
                result = (string)obj["drugType"];
            }
        }

        return result;
    }

    private async void LoopUntilPlayerIsOutOfRange(Vehicle veh)
    {
        while (true)
        {
            if (_menu.Visible == false) break;
            var trunk = API.GetEntityBoneIndexByName(veh.Handle, "boot");
            Vector3 coords = API.GetWorldPositionOfEntityBone(veh.Handle, trunk);
            if (Game.PlayerPed.Position.DistanceTo(coords) >= 1.5f)
            {
                _menu.Visible = false;
                break;
            }

            await BaseScript.Delay(1000);
        }
    }
}