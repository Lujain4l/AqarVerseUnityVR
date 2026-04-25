using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HouseFilterManager : MonoBehaviour
{
    [Header("Dropdowns")]
    public TMP_Dropdown cityDropdown;
    public TMP_Dropdown districtDropdown;
    public TMP_Dropdown buildingTypeDropdown;

    [Header("Houses")]
    public List<HouseData> houses = new List<HouseData>();

    [Header("UI Warning")]
    public GameObject warningText;

    void Start()
    {
        cityDropdown.onValueChanged.AddListener(delegate { FilterHouses(); });
        districtDropdown.onValueChanged.AddListener(delegate { FilterHouses(); });
        buildingTypeDropdown.onValueChanged.AddListener(delegate { FilterHouses(); });

        FilterHouses();
    }

    public void FilterHouses()
    {
        string selectedCity = cityDropdown.options[cityDropdown.value].text;
        string selectedDistrict = districtDropdown.options[districtDropdown.value].text;
        string selectedType = buildingTypeDropdown.options[buildingTypeDropdown.value].text;

      
        bool districtSelectedWithoutCity =
            selectedDistrict != "All" &&
            selectedDistrict != "" &&
            (selectedCity == "All" || selectedCity == "");

        warningText.SetActive(districtSelectedWithoutCity);

        foreach (HouseData house in houses)
        {
            bool show = true;

           
            if (selectedCity != "All" && selectedCity != "")
            {
                if (house.city != selectedCity)
                    show = false;
            }

         
            if (selectedDistrict != "All" && selectedDistrict != "")
            {
                if (selectedCity == "All" || selectedCity == "")
                {
                    show = false;
                }
                else if (house.district != selectedDistrict)
                {
                    show = false;
                }
            }

         
            if (selectedType != "All" && selectedType != "")
            {
                if (house.buildingType != selectedType)
                    show = false;
            }

   
            house.gameObject.SetActive(show);
        }
    }
}
