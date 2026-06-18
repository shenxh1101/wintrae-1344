using System;
using System.Collections.Generic;

namespace FinanceReimbursement.Models
{
    public class ReimbursementStandard
    {
        public Dictionary<EmployeeLevel, decimal> DailySubsidy { get; set; } = new Dictionary<EmployeeLevel, decimal>();

        public Dictionary<CityLevel, decimal> CitySubsidyMultiplier { get; set; } = new Dictionary<CityLevel, decimal>();

        public Dictionary<EmployeeLevel, decimal> MealStandard { get; set; } = new Dictionary<EmployeeLevel, decimal>();

        public Dictionary<EmployeeLevel, Dictionary<CityLevel, decimal>> AccommodationStandard { get; set; }
            = new Dictionary<EmployeeLevel, Dictionary<CityLevel, decimal>>();

        public Dictionary<EmployeeLevel, Dictionary<TransportationType, decimal?>> TransportationStandard { get; set; }
            = new Dictionary<EmployeeLevel, Dictionary<TransportationType, decimal?>>();

        public Dictionary<EmployeeLevel, Dictionary<CityLevel, decimal>> TransportationDailyCap { get; set; }
            = new Dictionary<EmployeeLevel, Dictionary<CityLevel, decimal>>();

        public Dictionary<ExpenseCategory, decimal> CategoryCapPerRequest { get; set; } = new Dictionary<ExpenseCategory, decimal>();

        public decimal TaxiDailyLimit { get; set; } = 200m;

        public int MaxDaysPerTrip { get; set; } = 30;

        public bool RequireInvoiceForOver { get; set; } = true;

        public decimal InvoiceRequiredThreshold { get; set; } = 50m;

        public static ReimbursementStandard CreateDefault()
        {
            var standard = new ReimbursementStandard();

            standard.DailySubsidy[EmployeeLevel.Junior] = 80;
            standard.DailySubsidy[EmployeeLevel.Supervisor] = 100;
            standard.DailySubsidy[EmployeeLevel.Manager] = 150;
            standard.DailySubsidy[EmployeeLevel.Director] = 200;
            standard.DailySubsidy[EmployeeLevel.VicePresident] = 300;
            standard.DailySubsidy[EmployeeLevel.President] = 500;

            standard.CitySubsidyMultiplier[CityLevel.Tier1] = 1.5m;
            standard.CitySubsidyMultiplier[CityLevel.Tier2] = 1.2m;
            standard.CitySubsidyMultiplier[CityLevel.Tier3] = 1.0m;
            standard.CitySubsidyMultiplier[CityLevel.Tier4] = 0.8m;

            standard.MealStandard[EmployeeLevel.Junior] = 80;
            standard.MealStandard[EmployeeLevel.Supervisor] = 100;
            standard.MealStandard[EmployeeLevel.Manager] = 150;
            standard.MealStandard[EmployeeLevel.Director] = 200;
            standard.MealStandard[EmployeeLevel.VicePresident] = 300;
            standard.MealStandard[EmployeeLevel.President] = 500;

            foreach (EmployeeLevel level in Enum.GetValues(typeof(EmployeeLevel)))
            {
                standard.AccommodationStandard[level] = new Dictionary<CityLevel, decimal>();
                standard.TransportationDailyCap[level] = new Dictionary<CityLevel, decimal>();
            }

            standard.AccommodationStandard[EmployeeLevel.Junior][CityLevel.Tier1] = 400;
            standard.AccommodationStandard[EmployeeLevel.Junior][CityLevel.Tier2] = 300;
            standard.AccommodationStandard[EmployeeLevel.Junior][CityLevel.Tier3] = 250;
            standard.AccommodationStandard[EmployeeLevel.Junior][CityLevel.Tier4] = 200;

            standard.AccommodationStandard[EmployeeLevel.Supervisor][CityLevel.Tier1] = 500;
            standard.AccommodationStandard[EmployeeLevel.Supervisor][CityLevel.Tier2] = 400;
            standard.AccommodationStandard[EmployeeLevel.Supervisor][CityLevel.Tier3] = 300;
            standard.AccommodationStandard[EmployeeLevel.Supervisor][CityLevel.Tier4] = 250;

            standard.AccommodationStandard[EmployeeLevel.Manager][CityLevel.Tier1] = 700;
            standard.AccommodationStandard[EmployeeLevel.Manager][CityLevel.Tier2] = 550;
            standard.AccommodationStandard[EmployeeLevel.Manager][CityLevel.Tier3] = 450;
            standard.AccommodationStandard[EmployeeLevel.Manager][CityLevel.Tier4] = 350;

            standard.AccommodationStandard[EmployeeLevel.Director][CityLevel.Tier1] = 900;
            standard.AccommodationStandard[EmployeeLevel.Director][CityLevel.Tier2] = 700;
            standard.AccommodationStandard[EmployeeLevel.Director][CityLevel.Tier3] = 550;
            standard.AccommodationStandard[EmployeeLevel.Director][CityLevel.Tier4] = 450;

            standard.AccommodationStandard[EmployeeLevel.VicePresident][CityLevel.Tier1] = 1200;
            standard.AccommodationStandard[EmployeeLevel.VicePresident][CityLevel.Tier2] = 1000;
            standard.AccommodationStandard[EmployeeLevel.VicePresident][CityLevel.Tier3] = 800;
            standard.AccommodationStandard[EmployeeLevel.VicePresident][CityLevel.Tier4] = 600;

            standard.AccommodationStandard[EmployeeLevel.President][CityLevel.Tier1] = 2000;
            standard.AccommodationStandard[EmployeeLevel.President][CityLevel.Tier2] = 1500;
            standard.AccommodationStandard[EmployeeLevel.President][CityLevel.Tier3] = 1200;
            standard.AccommodationStandard[EmployeeLevel.President][CityLevel.Tier4] = 1000;

            foreach (EmployeeLevel level in Enum.GetValues(typeof(EmployeeLevel)))
            {
                foreach (CityLevel city in Enum.GetValues(typeof(CityLevel)))
                {
                    decimal multiplier = standard.CitySubsidyMultiplier[city];
                    standard.TransportationDailyCap[level][city] = 200 * multiplier;
                }
            }

            standard.TransportationDailyCap[EmployeeLevel.Manager][CityLevel.Tier1] = 400;
            standard.TransportationDailyCap[EmployeeLevel.Director][CityLevel.Tier1] = 600;
            standard.TransportationDailyCap[EmployeeLevel.VicePresident][CityLevel.Tier1] = 1000;
            standard.TransportationDailyCap[EmployeeLevel.President][CityLevel.Tier1] = 2000;

            foreach (EmployeeLevel level in Enum.GetValues(typeof(EmployeeLevel)))
            {
                standard.TransportationStandard[level] = new Dictionary<TransportationType, decimal?>();
                foreach (TransportationType t in Enum.GetValues(typeof(TransportationType)))
                {
                    standard.TransportationStandard[level][t] = null;
                }
            }

            standard.TransportationStandard[EmployeeLevel.Junior][TransportationType.Airplane] = null;
            standard.TransportationStandard[EmployeeLevel.Supervisor][TransportationType.Airplane] = null;
            standard.TransportationStandard[EmployeeLevel.Manager][TransportationType.Airplane] = 2000;
            standard.TransportationStandard[EmployeeLevel.Director][TransportationType.Airplane] = 3000;
            standard.TransportationStandard[EmployeeLevel.VicePresident][TransportationType.Airplane] = 5000;
            standard.TransportationStandard[EmployeeLevel.President][TransportationType.Airplane] = null;

            standard.CategoryCapPerRequest[ExpenseCategory.Entertainment] = 5000;
            standard.CategoryCapPerRequest[ExpenseCategory.Conference] = 20000;
            standard.CategoryCapPerRequest[ExpenseCategory.Training] = 10000;
            standard.CategoryCapPerRequest[ExpenseCategory.Other] = 3000;

            return standard;
        }
    }
}
