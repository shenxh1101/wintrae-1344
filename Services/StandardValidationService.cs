using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Services
{
    public class StandardValidationService
    {
        private readonly ReimbursementStandard _standard;

        public StandardValidationService(ReimbursementStandard? standard = null)
        {
            _standard = standard ?? ReimbursementStandard.CreateDefault();
        }

        public ValidationResult Validate(ReimbursementForm form)
        {
            var result = new ValidationResult();

            if (form == null)
            {
                result.AddError("FORM_NULL", "报销单不能为空", "基础校验");
                return result;
            }

            ValidateBasic(form, result);
            ValidateAccommodation(form, result);
            ValidateMeal(form, result);
            ValidateTransportation(form, result);
            ValidateTripDays(form, result);
            ValidateCategoryCap(form, result);

            return result;
        }

        private void ValidateBasic(ReimbursementForm form, ValidationResult result)
        {
            if (form.Applicant == null || string.IsNullOrWhiteSpace(form.Applicant.Id))
            {
                result.AddError("APPLICANT_REQUIRED", "申请人信息必填", "基础校验");
            }

            if (string.IsNullOrWhiteSpace(form.Title))
            {
                result.AddWarning("TITLE_EMPTY", "建议填写报销单标题", "基础校验");
            }

            if (form.ExpenseItems == null || form.ExpenseItems.Count == 0)
            {
                result.AddError("NO_EXPENSE_ITEMS", "至少需要一条费用明细", "基础校验");
            }
            else
            {
                foreach (var item in form.ExpenseItems)
                {
                    if (item.Amount <= 0)
                    {
                        result.AddError("AMOUNT_INVALID", $"费用明细[{item.Description ?? item.Category.ToString()}]金额必须大于0",
                            "基础校验", item.Id);
                    }

                    if (item.ExpenseDate == default)
                    {
                        result.AddWarning("DATE_EMPTY", $"费用明细[{item.Description ?? item.Category.ToString()}]缺少费用日期",
                            "基础校验", item.Id);
                    }
                }
            }

            if (form.Trips != null && form.Trips.Count > 0)
            {
                foreach (var trip in form.Trips)
                {
                    if (trip.ReturnDate < trip.DepartureDate)
                    {
                        result.AddError("TRIP_DATE_INVALID", $"行程[{trip.Destination}]返程日期不能早于出发日期",
                            "行程校验", trip.Id);
                    }
                }
            }
        }

        private void ValidateAccommodation(ReimbursementForm form, ValidationResult result)
        {
            if (form.Trips == null || form.Trips.Count == 0) return;
            if (form.Applicant == null) return;

            var level = form.Applicant.Level;

            foreach (var trip in form.Trips)
            {
                var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
                var standard = GetAccommodationStandard(level, cityLevel);
                var nights = Math.Max(0, trip.Days - 1);

                if (nights <= 0) continue;

                var maxTotal = standard * nights;

                decimal actualTotal = 0;
                foreach (var item in form.ExpenseItems)
                {
                    if (item.Category == ExpenseCategory.Accommodation &&
                        item.TripId == trip.Id)
                    {
                        actualTotal += item.TotalAmount;
                    }
                }

                if (actualTotal > maxTotal)
                {
                    var overAmount = actualTotal - maxTotal;
                    result.AddWarning("ACCOMMODATION_OVER_LIMIT",
                        $"行程[{trip.Destination}]住宿费超标，标准{maxTotal:N2}元({nights}晚×{standard:N2}元)，实际{actualTotal:N2}元，超标{overAmount:N2}元",
                        "超标校验", trip.Id, maxTotal, actualTotal,
                        "请确认超标原因或调整住宿标准");
                }
            }
        }

        private void ValidateMeal(ReimbursementForm form, ValidationResult result)
        {
            if (form.Trips == null || form.Trips.Count == 0) return;
            if (form.Applicant == null) return;

            var level = form.Applicant.Level;

            foreach (var trip in form.Trips)
            {
                var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
                var multiplier = _standard.CitySubsidyMultiplier.ContainsKey(cityLevel)
                    ? _standard.CitySubsidyMultiplier[cityLevel] : 1.0m;
                var baseStandard = _standard.MealStandard.ContainsKey(level)
                    ? _standard.MealStandard[level] : 80m;
                var standard = baseStandard * multiplier;
                var days = trip.Days;

                if (days <= 0) continue;

                var maxTotal = standard * days;

                decimal actualTotal = 0;
                foreach (var item in form.ExpenseItems)
                {
                    if (item.Category == ExpenseCategory.Meal &&
                        item.TripId == trip.Id)
                    {
                        actualTotal += item.TotalAmount;
                    }
                }

                if (actualTotal > maxTotal)
                {
                    var overAmount = actualTotal - maxTotal;
                    result.AddWarning("MEAL_OVER_LIMIT",
                        $"行程[{trip.Destination}]餐饮费超标，标准{maxTotal:N2}元({days}天×{standard:N2}元)，实际{actualTotal:N2}元，超标{overAmount:N2}元",
                        "超标校验", trip.Id, maxTotal, actualTotal,
                        "建议提供招待说明或从补贴中抵扣");
                }
            }
        }

        private void ValidateTransportation(ReimbursementForm form, ValidationResult result)
        {
            if (form.Applicant == null) return;

            var level = form.Applicant.Level;

            foreach (var item in form.ExpenseItems)
            {
                if (item.Category != ExpenseCategory.Transportation) continue;
                if (item.TransportationType == null) continue;

                var transportType = item.TransportationType.Value;

                if (_standard.TransportationStandard.ContainsKey(level) &&
                    _standard.TransportationStandard[level].ContainsKey(transportType))
                {
                    var standard = _standard.TransportationStandard[level][transportType];
                    if (standard.HasValue && item.TotalAmount > standard.Value)
                    {
                        var over = item.TotalAmount - standard.Value;
                        result.AddWarning("TRANSPORT_OVER_LIMIT",
                            $"交通费[{item.Description ?? transportType.ToString()}]超标，标准{standard.Value:N2}元，实际{item.TotalAmount:N2}元，超标{over:N2}元",
                            "超标校验", item.Id, standard.Value, item.TotalAmount,
                            "建议使用经济舱/二等座等标准交通方式");
                    }
                }
            }

            if (form.Trips != null)
            {
                foreach (var trip in form.Trips)
                {
                    var cityLevel = trip.CityLevel ?? CityLevel.Tier2;
                    var dailyCap = GetTransportationDailyCap(level, cityLevel);
                    var days = trip.Days;
                    var maxTotal = dailyCap * days;

                    decimal actualTotal = 0;
                    foreach (var item in form.ExpenseItems)
                    {
                        if (item.Category == ExpenseCategory.Transportation &&
                            (item.TransportationType == TransportationType.Taxi ||
                             item.TransportationType == TransportationType.Subway) &&
                            item.TripId == trip.Id)
                        {
                            actualTotal += item.TotalAmount;
                        }
                    }

                    if (days > 0 && actualTotal > maxTotal)
                    {
                        var over = actualTotal - maxTotal;
                        result.AddWarning("LOCAL_TRANSPORT_OVER_LIMIT",
                            $"行程[{trip.Destination}]市内交通费超标，标准{maxTotal:N2}元({days}天×{dailyCap:N2}元)，实际{actualTotal:N2}元，超标{over:N2}元",
                            "超标校验", trip.Id, maxTotal, actualTotal,
                            "建议优先使用公共交通或提供超标说明");
                    }
                }
            }
        }

        private void ValidateTripDays(ReimbursementForm form, ValidationResult result)
        {
            if (form.Trips == null) return;

            foreach (var trip in form.Trips)
            {
                if (trip.Days > _standard.MaxDaysPerTrip)
                {
                    result.AddWarning("TRIP_DAYS_TOO_LONG",
                        $"行程[{trip.Destination}]时长{trip.Days}天超过标准{_standard.MaxDaysPerTrip}天，建议分批报销",
                        "行程校验", trip.Id);
                }
            }
        }

        private void ValidateCategoryCap(ReimbursementForm form, ValidationResult result)
        {
            var categorySummary = form.GetCategorySummary();

            foreach (var kv in categorySummary)
            {
                if (_standard.CategoryCapPerRequest.ContainsKey(kv.Key))
                {
                    var cap = _standard.CategoryCapPerRequest[kv.Key];
                    if (kv.Value > cap)
                    {
                        result.AddWarning("CATEGORY_OVER_CAP",
                            $"{GetCategoryName(kv.Key)}单次报销限额{cap:N2}元，本次{kv.Value:N2}元，超额{kv.Value - cap:N2}元",
                            "超标校验", expected: cap, actual: kv.Value,
                            suggestion: "建议拆分多次报销或提交特殊审批");
                    }
                }
            }
        }

        private decimal GetAccommodationStandard(EmployeeLevel level, CityLevel city)
        {
            if (_standard.AccommodationStandard.ContainsKey(level) &&
                _standard.AccommodationStandard[level].ContainsKey(city))
            {
                return _standard.AccommodationStandard[level][city];
            }
            return 300m;
        }

        private decimal GetTransportationDailyCap(EmployeeLevel level, CityLevel city)
        {
            if (_standard.TransportationDailyCap.ContainsKey(level) &&
                _standard.TransportationDailyCap[level].ContainsKey(city))
            {
                return _standard.TransportationDailyCap[level][city];
            }
            return 200m;
        }

        private string GetCategoryName(ExpenseCategory category)
        {
            switch (category)
            {
                case ExpenseCategory.Transportation: return "交通费";
                case ExpenseCategory.Accommodation: return "住宿费";
                case ExpenseCategory.Meal: return "餐饮费";
                case ExpenseCategory.Communication: return "通讯费";
                case ExpenseCategory.Office: return "办公费";
                case ExpenseCategory.Entertainment: return "招待费";
                case ExpenseCategory.Conference: return "会议费";
                case ExpenseCategory.Training: return "培训费";
                default: return "其他费用";
            }
        }
    }
}
