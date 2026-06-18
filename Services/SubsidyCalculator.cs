using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Services
{
    public class SubsidyCalculator
    {
        private readonly ReimbursementStandard _standard;

        public SubsidyCalculator(ReimbursementStandard? standard = null)
        {
            _standard = standard ?? ReimbursementStandard.CreateDefault();
        }

        public decimal CalculateDailySubsidy(EmployeeLevel level, CityLevel cityLevel)
        {
            var baseSubsidy = _standard.DailySubsidy.ContainsKey(level)
                ? _standard.DailySubsidy[level] : 80m;
            var multiplier = _standard.CitySubsidyMultiplier.ContainsKey(cityLevel)
                ? _standard.CitySubsidyMultiplier[cityLevel] : 1.0m;

            return Math.Round(baseSubsidy * multiplier, 2);
        }

        public decimal CalculateTripSubsidy(Trip trip, EmployeeLevel level)
        {
            if (trip == null || trip.Days <= 0) return 0;

            var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
            var daily = CalculateDailySubsidy(level, cityLevel);

            return Math.Round(daily * trip.Days, 2);
        }

        public decimal CalculateTotalSubsidy(ReimbursementForm form)
        {
            if (form == null || form.Applicant == null) return 0;
            if (form.Trips == null || form.Trips.Count == 0) return 0;

            decimal total = 0;
            foreach (var trip in form.Trips)
            {
                total += CalculateTripSubsidy(trip, form.Applicant.Level);
            }

            return Math.Round(total, 2);
        }

        public SubsidyBreakdown GetSubsidyBreakdown(ReimbursementForm form)
        {
            var breakdown = new SubsidyBreakdown();
            if (form == null || form.Applicant == null || form.Trips == null) return breakdown;

            breakdown.TotalDays = 0;
            breakdown.Trips = new List<TripSubsidyDetail>();

            foreach (var trip in form.Trips)
            {
                var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
                var daily = CalculateDailySubsidy(form.Applicant.Level, cityLevel);
                var tripSubsidy = CalculateTripSubsidy(trip, form.Applicant.Level);

                breakdown.TotalDays += trip.Days;
                breakdown.TotalAmount += tripSubsidy;

                breakdown.Trips.Add(new TripSubsidyDetail
                {
                    TripId = trip.Id,
                    Destination = trip.Destination,
                    CityLevel = cityLevel,
                    Days = trip.Days,
                    DailySubsidy = daily,
                    TotalSubsidy = tripSubsidy,
                    Description = $"{trip.Destination} {trip.Days}天 × {daily:N2}元/天 = {tripSubsidy:N2}元"
                });
            }

            breakdown.Summary = $"共{breakdown.TotalDays}天，补贴合计{breakdown.TotalAmount:N2}元";

            return breakdown;
        }

        public decimal CalculateMealDeduction(ReimbursementForm form)
        {
            if (form == null || form.Trips == null || form.Applicant == null) return 0;

            decimal excessDeduction = 0;

            foreach (var trip in form.Trips)
            {
                var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
                var multiplier = _standard.CitySubsidyMultiplier.ContainsKey(cityLevel)
                    ? _standard.CitySubsidyMultiplier[cityLevel] : 1.0m;
                var baseStandard = _standard.MealStandard.ContainsKey(form.Applicant.Level)
                    ? _standard.MealStandard[form.Applicant.Level] : 80m;
                var standard = baseStandard * multiplier;
                var days = trip.Days;

                if (days <= 0) continue;

                var maxTotal = standard * days;

                decimal actualTotal = 0;
                foreach (var item in form.ExpenseItems)
                {
                    if (item.Category == ExpenseCategory.Meal && item.TripId == trip.Id)
                    {
                        actualTotal += item.TotalAmount;
                    }
                }

                if (actualTotal > maxTotal)
                {
                    excessDeduction += actualTotal - maxTotal;
                }
            }

            return Math.Round(excessDeduction, 2);
        }
    }

    public class SubsidyBreakdown
    {
        public int TotalDays { get; set; }
        public decimal TotalAmount { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<TripSubsidyDetail> Trips { get; set; } = new List<TripSubsidyDetail>();
    }

    public class TripSubsidyDetail
    {
        public string TripId { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public CityLevel CityLevel { get; set; }
        public int Days { get; set; }
        public decimal DailySubsidy { get; set; }
        public decimal TotalSubsidy { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
