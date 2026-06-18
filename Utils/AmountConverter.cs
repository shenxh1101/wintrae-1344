using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceReimbursement.Utils
{
    public static class AmountConverter
    {
        private static readonly string[] Digits = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
        private static readonly string[] IntUnits = { "", "拾", "佰", "仟" };
        private static readonly string[] SectionUnits = { "", "万", "亿", "万亿" };
        private static readonly string[] DecUnits = { "角", "分" };

        public static string ToChineseAmount(decimal amount)
        {
            if (amount == 0) return "零元整";

            string result = "";
            bool isNegative = amount < 0;
            amount = Math.Abs(amount);

            long integerPart = (long)Math.Truncate(amount);
            int decimalPart = (int)Math.Round((amount - integerPart) * 100);

            if (integerPart > 0)
            {
                result = ConvertIntegerPart(integerPart) + "元";
            }

            string decimalStr = ConvertDecimalPart(decimalPart);

            if (string.IsNullOrEmpty(decimalStr))
            {
                result += "整";
            }
            else
            {
                if (integerPart == 0)
                {
                    result = decimalStr;
                }
                else if (decimalPart < 10 && decimalPart > 0)
                {
                    result += "零" + decimalStr;
                }
                else
                {
                    result += decimalStr;
                }
            }

            if (isNegative)
            {
                result = "负" + result;
            }

            return result;
        }

        private static string ConvertIntegerPart(long number)
        {
            if (number == 0) return "零";

            string result = "";
            int sectionIndex = 0;
            bool hasNonZero = false;
            bool needZero = false;

            while (number > 0)
            {
                int section = (int)(number % 10000);
                number /= 10000;

                if (section == 0)
                {
                    if (hasNonZero)
                    {
                        needZero = true;
                    }
                }
                else
                {
                    string sectionStr = ConvertSection(section);
                    if (needZero)
                    {
                        result = "零" + result;
                        needZero = false;
                    }
                    result = sectionStr + SectionUnits[sectionIndex] + result;
                    hasNonZero = true;
                }

                sectionIndex++;
            }

            return result;
        }

        private static string ConvertSection(int section)
        {
            if (section == 0) return "";

            string result = "";
            bool hasNonZero = false;
            bool needZero = false;
            int digitIndex = 0;

            while (section > 0 || digitIndex < 4)
            {
                int digit = section % 10;
                section /= 10;

                if (digit == 0)
                {
                    if (hasNonZero)
                    {
                        needZero = true;
                    }
                }
                else
                {
                    if (needZero)
                    {
                        result = "零" + result;
                        needZero = false;
                    }
                    result = Digits[digit] + IntUnits[digitIndex] + result;
                    hasNonZero = true;
                }

                digitIndex++;
            }

            return result;
        }

        private static string ConvertDecimalPart(int decimalPart)
        {
            if (decimalPart == 0) return "";

            string result = "";
            int jiao = decimalPart / 10;
            int fen = decimalPart % 10;

            if (jiao > 0)
            {
                result += Digits[jiao] + DecUnits[0];
            }

            if (fen > 0)
            {
                if (jiao == 0)
                {
                    result += "零";
                }
                result += Digits[fen] + DecUnits[1];
            }

            return result;
        }

        public static string FormatAmount(decimal amount, string currency = "CNY")
        {
            string symbol = currency switch
            {
                "CNY" => "¥",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                "JPY" => "¥",
                "HKD" => "HK$",
                _ => ""
            };
            return $"{symbol}{amount:N2}";
        }

        public static string ToEnglishAmount(decimal amount)
        {
            if (amount == 0) return "Zero Dollars Only";

            bool isNegative = amount < 0;
            amount = Math.Abs(amount);

            long dollars = (long)Math.Truncate(amount);
            int cents = (int)Math.Round((amount - dollars) * 100);

            string result = ConvertDollarsToEnglish(dollars);

            if (cents > 0)
            {
                result += $" and {ConvertCentsToEnglish(cents)}";
            }
            else
            {
                result += " Only";
            }

            if (isNegative)
            {
                result = "Negative " + result;
            }

            return result;
        }

        private static string ConvertDollarsToEnglish(long number)
        {
            if (number == 0) return "Zero Dollars";

            string result = "";

            long trillion = 1000000000000;
            long billion = 1000000000;
            long million = 1000000;
            long thousand = 1000;

            if (number >= trillion)
            {
                long n = number / trillion;
                result += ConvertHundreds(n) + " Trillion ";
                number %= trillion;
            }

            if (number >= billion)
            {
                long n = number / billion;
                result += ConvertHundreds(n) + " Billion ";
                number %= billion;
            }

            if (number >= million)
            {
                long n = number / million;
                result += ConvertHundreds(n) + " Million ";
                number %= million;
            }

            if (number >= thousand)
            {
                long n = number / thousand;
                result += ConvertHundreds(n) + " Thousand ";
                number %= thousand;
            }

            if (number > 0)
            {
                result += ConvertHundreds(number);
            }

            result = result.Trim() + " Dollars";
            return result;
        }

        private static string ConvertHundreds(long number)
        {
            string result = "";
            long hundreds = number / 100;
            long remainder = number % 100;

            if (hundreds > 0)
            {
                result += ConvertUnits(hundreds) + " Hundred";
                if (remainder > 0) result += " ";
            }

            if (remainder > 0)
            {
                if (remainder < 20)
                {
                    result += ConvertUnits(remainder);
                }
                else
                {
                    long tens = remainder / 10;
                    long ones = remainder % 10;
                    result += ConvertTens(tens);
                    if (ones > 0)
                    {
                        result += "-" + ConvertUnits(ones);
                    }
                }
            }

            return result;
        }

        private static string ConvertUnits(long number)
        {
            return number switch
            {
                0 => "",
                1 => "One",
                2 => "Two",
                3 => "Three",
                4 => "Four",
                5 => "Five",
                6 => "Six",
                7 => "Seven",
                8 => "Eight",
                9 => "Nine",
                10 => "Ten",
                11 => "Eleven",
                12 => "Twelve",
                13 => "Thirteen",
                14 => "Fourteen",
                15 => "Fifteen",
                16 => "Sixteen",
                17 => "Seventeen",
                18 => "Eighteen",
                19 => "Nineteen",
                _ => ""
            };
        }

        private static string ConvertTens(long number)
        {
            return number switch
            {
                2 => "Twenty",
                3 => "Thirty",
                4 => "Forty",
                5 => "Fifty",
                6 => "Sixty",
                7 => "Seventy",
                8 => "Eighty",
                9 => "Ninety",
                _ => ""
            };
        }

        private static string ConvertCentsToEnglish(int cents)
        {
            if (cents == 0) return "";
            return $"{ConvertHundreds(cents).Replace(" Dollars", "")} Cents";
        }
    }
}
