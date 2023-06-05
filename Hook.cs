using HarmonyLib;
using System;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using Il2CppSystem;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DensityMod
{
    internal class DensityModHook{

        static public GameObject dropdownDenLow;
        static public GameObject dropdownDenHigh;
        static public GameObject dropdownValueLow;
        static public GameObject dropdownValueHigh;
        static public GameObject densityMenu;
        static public GameObject menuContainer;
        static public GameObject GenerateCityMenu;
        static public GameObject DensityModBtn;

        //This is to convert the Density enum to a clean looking string
        static public string EnumToString<T>(T e) where T : System.Enum{
            var s = e.ToString();
            s = Regex.Replace(s, "(\\B[A-Z])", " $1");
            s = new CultureInfo("en-US",false).TextInfo.ToTitleCase(s);
            return s;
        }

        //Converts string into the Density Enum
        static public T StringToEnum<T>(string s) where T : System.Enum{
            s = s.Substring(0,1).ToLower() + s.Substring(1).Replace(" ", ""); //Makes the first letter lowercase(Used in the case of veryHigh, as well as removing the space)
            return (T)System.Enum.Parse(typeof(T), s);
        }

        //Update the city generation with the desired density values
        [HarmonyPatch(typeof(CityConstructor), "Update")]
        public class Citydata_PopulationMultiplier{
            public static void Postfix(){
                CityTile[] cityTile = Resources.FindObjectsOfTypeAll<CityTile>();
                CityConstructor cityConstructor = Resources.FindObjectsOfTypeAll<CityConstructor>()[0];
                //Gets the values from the config file and stores them in a Vector 2, because two ints would be too easy... and messy
                var DensityRange = new Vector2(
                    (int)System.Enum.Parse(typeof(BuildingPreset.Density), DensityMod.DensityMinConfig.Value),
                    (int)System.Enum.Parse(typeof(BuildingPreset.Density), DensityMod.DensityMaxConfig.Value));

                var ValueRange = new Vector2(
                    (int)System.Enum.Parse(typeof(BuildingPreset.LandValue), DensityMod.LandValueMinConfig.Value),
                    (int)System.Enum.Parse(typeof(BuildingPreset.LandValue), DensityMod.LandValueMaxConfig.Value));

                //Uses the valueRange and clamps the allowed density. This is done by treating the Enum as an int, since the values range from 0 - 3.
                if (cityConstructor.generateNew){
                    foreach(CityTile tile in cityTile){
                        if (tile.name == "CityTile") return;//This was used, because there seems to be a CityTile manager or something, so I didn't want to edit it(Also caused issues before)
                        var d = (int)tile.density;
                        d = (int)System.Math.Clamp(d,DensityRange.x, DensityRange.y);

                        var v = (int)tile.landValue;
                        v = (int)System.Math.Clamp(v,ValueRange.x, ValueRange.y);
                        
                        //Density Update
                        tile.density = (BuildingPreset.Density)d;
                        DensityMod.Logger.LogDebug($"{tile.name} density = {tile.density}");
                        
                        //Land Value update
                        tile.landValue = (BuildingPreset.LandValue)v;
                        DensityMod.Logger.LogDebug($"{tile.name} landValue = {tile.landValue}");                    
                    }
                }
            }
        }

        //Simple function to close the Density menu
        static void CloseDensityMenu(){
            densityMenu.SetActive(false);
            GenerateCityMenu.SetActive(true);
        }

        //Simple function to open the Density menu(duh)
        static void OpenDensityMenu(){
            densityMenu.SetActive(true);
            GenerateCityMenu.SetActive(false);
        }

        //Creates the menu objects
        [HarmonyPatch(typeof(MainMenuController), "Start")]
        public class MainMenuController_Start{

            public static void Postfix(){
                var inputTemplate = GameObject.Find("MenuCanvas").transform.Find("MainMenu/GenerateCityPanel/GenerateNewCityComponents/SizeDropdown").gameObject;
                GenerateCityMenu = GameObject.Find("MenuCanvas").transform.Find("MainMenu/GenerateCityPanel/").gameObject;

                //Dropdown list of options.
                List<string> DensityOptions = new List<string>();
                    DensityOptions.Add("Low");
                    DensityOptions.Add("Medium");
                    DensityOptions.Add("High");
                    DensityOptions.Add("Very High");

                List<string> ValueOptions = new List<string>();
                    ValueOptions.Add("Very Low");
                    ValueOptions.Add("Low");
                    ValueOptions.Add("Medium");
                    ValueOptions.Add("High");
                    ValueOptions.Add("Very High");

                if (GenerateCityMenu != null){
                    //Generate a density menu, based on the Generate City menu already present
                    densityMenu = GameObject.Instantiate(GenerateCityMenu.gameObject);
                    densityMenu.name = "Density Menu";
                    densityMenu.transform.parent = GenerateCityMenu.transform.parent;
                    densityMenu.transform.localPosition = new Vector3(0, -42, 0);//Set the localPosition to this, because when cloned it starts in an odd position.

                    //Finds the gameobject that holds the dropdowns
                    menuContainer = densityMenu.transform.FindChild("GenerateNewCityComponents").gameObject;
                    menuContainer.name = "DensitySettingsComponents";

                    //Removes all elements currently present
                    //Note: it was done in this way, because a while and foreach loop would fail. It seems when this is created, the data is static, 
                    //      so when you remove Child(0), Child(1) does not become the new Child(0) as you would expect in Unity.
                    for(int i = 0; i < menuContainer.transform.GetChildCount(); i++){
                        GameObject.Destroy(menuContainer.transform.GetChild(i).gameObject);
                    }

                    //Removes one of the buttons from the menu, then changes the remaining one to be the done button
                    GameObject.Destroy(densityMenu.transform.FindChild("ButtonArea").GetChild(0).gameObject);
                    var doneButton = densityMenu.transform.FindChild("ButtonArea").GetChild(1).gameObject;
                    doneButton.transform.parent = densityMenu.transform.FindChild("ButtonArea").transform;
                    doneButton.GetComponent<Button>().onClick.AddListener((UnityAction)CloseDensityMenu);

                    //Grabs a template button, then adds it to the Generate City menu, then sets itself to be between the back and continue buttons
                    var btnTemp = GenerateCityMenu.transform.Find("ButtonArea").GetChild(1);
                    DensityModBtn = GameObject.Instantiate(btnTemp.gameObject);
                    DensityModBtn.transform.parent = btnTemp.transform.parent;
                    DensityModBtn.transform.SetSiblingIndex(1);
                    DensityModBtn.GetComponent<Button>().onClick.AddListener((UnityAction)OpenDensityMenu);
                }

                if (inputTemplate != null){
                    //Low Density dropdown                   
                    var denLowChoice = EnumToString<BuildingPreset.Density>((BuildingPreset.Density)System.Enum.Parse(typeof(BuildingPreset.Density), DensityMod.DensityMinConfig.Value));
                    dropdownDenLow = createDropdown(inputTemplate.gameObject, "DensitydropdownLow", menuContainer.transform, DensityOptions, denLowChoice);

                    //High Density dropdown
                    var denHighChoice = EnumToString<BuildingPreset.Density>((BuildingPreset.Density)System.Enum.Parse(typeof(BuildingPreset.Density), DensityMod.DensityMaxConfig.Value));
                    dropdownDenHigh = createDropdown(inputTemplate.gameObject, "DensitydropdownHigh", menuContainer.transform, DensityOptions, denHighChoice);

                    //Lowest Land Value dropdown
                    var valueLowChoice = EnumToString<BuildingPreset.LandValue>((BuildingPreset.LandValue)System.Enum.Parse(typeof(BuildingPreset.LandValue), DensityMod.LandValueMinConfig.Value));
                    dropdownValueLow = createDropdown(inputTemplate.gameObject, "LandValueDropdownLow", menuContainer.transform, ValueOptions, valueLowChoice);

                    //Highest Land Value dropdown
                    var valueHighChoice = EnumToString<BuildingPreset.LandValue>((BuildingPreset.LandValue)System.Enum.Parse(typeof(BuildingPreset.LandValue), DensityMod.LandValueMaxConfig.Value));
                    dropdownValueHigh = createDropdown(inputTemplate.gameObject, "LandValueDropdownHigh", menuContainer.transform, ValueOptions, valueHighChoice);
                }
            }
        }

        static public GameObject createDropdown(GameObject _template, string _name, Transform _container, List<string> _options, string _selectedOption){
            var final = GameObject.Instantiate(_template);
            final.gameObject.name = _name;
            final.transform.parent = _container;
            final.GetComponent<DropdownController>().AddOptions(_options, false);
            final.GetComponent<DropdownController>().SelectFromStaticOption(_selectedOption);
            return final;
        }

        //Failsafe to force texts to be updated. For some reason doing so on creation doesn't seem to function as expected.
        [HarmonyPatch(typeof(MainMenuController), "Update")]
        public class MainMenuController_Update{
            
            public static void Postfix(){
                if (dropdownDenLow != null){
                    //Lowest Density text
                    if (dropdownDenLow.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text != "Lowest Density"){
                        dropdownDenLow.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text = "Lowest Density";
                    }
                    //Highest Density text
                    if (dropdownDenHigh.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text != "Highest Density"){
                        dropdownDenHigh.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text = "Highest Density";
                    }
                    //Lowest Land Value text
                    if (dropdownValueLow.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text != "Lowest Land Value"){
                        dropdownValueLow.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text = "Lowest Land Value";
                    }
                    //Highest Land Value text
                    if (dropdownValueHigh.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text != "Highest Land Value"){
                        dropdownValueHigh.transform.FindChild("LabelText").GetComponent<TMPro.TextMeshProUGUI>().text = "Highest Land Value";
                    }
                    //Density Menu header title
                    if (densityMenu.transform.FindChild("Header").FindChild("PanelTitle").GetComponent<TMPro.TextMeshProUGUI>().text != "Density Settings"){
                        densityMenu.transform.FindChild("Header").FindChild("PanelTitle").GetComponent<TMPro.TextMeshProUGUI>().text = "Density Settings";
                    }
                    //Density Menu button text
                    if (densityMenu.transform.FindChild("ButtonArea/Continue/Text").GetComponent<TMPro.TextMeshProUGUI>().text != "Done"){
                        densityMenu.transform.FindChild("ButtonArea/Continue/Text").GetComponent<TMPro.TextMeshProUGUI>().text = "Done";
                    }
                    //Density Settings Button
                    if (DensityModBtn.transform.FindChild("Text").GetComponent<TMPro.TextMeshProUGUI>().text != "Density Settings"){
                        DensityModBtn.transform.FindChild("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "Density Settings";
                    }

                }                               
            }
        }

        [HarmonyPatch(typeof(DropdownController), "OnValueChange")]
        public class Dropdown_Update{
            public static void Postfix(){
                //Grabs the string from dropdown, then converts it to the specifed Enum used in the game
                BuildingPreset.Density enumDenLow = StringToEnum<BuildingPreset.Density>(dropdownDenLow.GetComponent<DropdownController>().GetCurrentSelectedStaticOption());
                BuildingPreset.Density enumDenHigh = StringToEnum<BuildingPreset.Density>(dropdownDenHigh.GetComponent<DropdownController>().GetCurrentSelectedStaticOption());
                BuildingPreset.LandValue enumLandLow = StringToEnum<BuildingPreset.LandValue>(dropdownValueLow.GetComponent<DropdownController>().GetCurrentSelectedStaticOption());
                BuildingPreset.LandValue enumLandHigh = StringToEnum<BuildingPreset.LandValue>(dropdownValueHigh.GetComponent<DropdownController>().GetCurrentSelectedStaticOption());
                //Had to mark the types from generic enum to a specific Enum, since the check below could not assume int from a generic Enum

                //Prevents players from setting the highest densty allowed, to be less than the lowest density allowed. It would break the clamp, and my head
                if ((int)enumDenLow > (int)enumDenHigh){
                    enumDenHigh = enumDenLow;
                    dropdownDenHigh.GetComponent<DropdownController>().SelectFromStaticOption(EnumToString(enumDenHigh));
                }
                if ((int)enumLandLow > (int)enumLandHigh){
                    enumLandHigh = enumLandLow;
                    dropdownDenHigh.GetComponent<DropdownController>().SelectFromStaticOption(EnumToString(enumLandHigh));
                }             


                //Update the config files
                DensityMod.DensityMinConfig.BoxedValue = enumDenLow.ToString();
                DensityMod.Logger.LogDebug($"Density Low Choice is: {dropdownDenLow.GetComponent<DropdownController>().GetCurrentSelectedStaticOption().ToString()}");
                DensityMod.DensityMaxConfig.BoxedValue = enumDenHigh.ToString();
                DensityMod.Logger.LogDebug($"Density High Choice is: {dropdownDenHigh.GetComponent<DropdownController>().GetCurrentSelectedStaticOption().ToString()}");

                //Land Value Config Update
                DensityMod.LandValueMinConfig.BoxedValue = enumLandLow.ToString();
                DensityMod.Logger.LogDebug($"Land Value Low Choice is: {dropdownValueLow.GetComponent<DropdownController>().GetCurrentSelectedStaticOption().ToString()}");
                DensityMod.LandValueMaxConfig.BoxedValue = enumLandHigh.ToString();
                DensityMod.Logger.LogDebug($"Land Value High Choice is: {dropdownValueHigh.GetComponent<DropdownController>().GetCurrentSelectedStaticOption().ToString()}");
            }
        }

    }

}