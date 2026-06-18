using System;
using System.Collections.Generic;
using Xunit;
using FinanceReimbursement;
using FinanceReimbursement.Models;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Tests
{
    public class AmountConverterTests
    {
        [Theory]
        [InlineData(0, "零元整")]
        [InlineData(1, "壹元整")]
        [InlineData(10, "壹拾元整")]
        [InlineData(100, "壹佰元整")]
        [InlineData(1000, "壹仟元整")]
        [InlineData(10000, "壹万元整")]
        [InlineData(12345.67, "壹万贰仟叁佰肆拾伍元陆角柒分")]
        [InlineData(100000000, "壹亿元整")]
        [InlineData(105.5, "壹佰零伍元伍角")]
        [InlineData(100.05, "壹佰元零伍分")]
        public void ToChineseAmount_ShouldConvertCorrectly(decimal amount, string expected)
        {
            var result = AmountConverter.ToChineseAmount(amount);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToChineseAmount_Negative_ShouldIncludeNegative()
        {
            var result = AmountConverter.ToChineseAmount(-123.45m);
            Assert.StartsWith("负", result);
        }

        [Fact]
        public void FormatAmount_ShouldUseCorrectSymbol()
        {
            Assert.Equal("¥1,234.56", AmountConverter.FormatAmount(1234.56m, "CNY"));
            Assert.Equal("$1,234.56", AmountConverter.FormatAmount(1234.56m, "USD"));
        }

        [Fact]
        public void ToEnglishAmount_BasicValues()
        {
            Assert.Contains("One Hundred Twenty Three Dollars", AmountConverter.ToEnglishAmount(123m));
            Assert.Contains("Forty Five Cents", AmountConverter.ToEnglishAmount(0.45m));
        }
    }
}
