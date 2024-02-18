using System;
using System.Reflection;
using LiZhenStandard.Extensions;

namespace LiZhenStandard
{
    public static class DataValidation
    {
        public static bool IsWrong(this object obj,out string wrongMessage)
        {
            PropertyInfo[] ppts = obj.GetType().GetProperties();
            wrongMessage = string.Empty;
            for (int i = 0; i < ppts.Length; i++)
            {
                PropertyInfo ppt = ppts[i];
                string pptName = ppt.Name;
                object pptValue = ppt.GetValue(obj);
                NumberLimitAttribute attNumber = ppt.GetCustomAttribute<NumberLimitAttribute>();
                TextLimitAttribute attText = ppt.GetCustomAttribute<TextLimitAttribute>();
                TimeLimitAttribute attTime = ppt.GetCustomAttribute<TimeLimitAttribute>();
                if (attNumber.IsNotNull())
                {
                    decimal value = 0;
                    try
                    {
                        value = Convert.ToDecimal(pptValue);
                    }
                    catch(Exception e) { wrongMessage = wrongMessage.AddLine("{0}错误：{1}",e.Message); }
                    if (value > attNumber.MaxNumber)
                        wrongMessage = wrongMessage.AddLine("{0}超过限定最大值:{1}。",pptName,attNumber.MaxNumber);
                    if (value > attNumber.MinNumber)
                        wrongMessage = wrongMessage.AddLine("{0}小于限定最小值:{1}。", pptName, attNumber.MinNumber);
                    if (value == 0 && !attNumber.AllowZero)
                        wrongMessage = wrongMessage.AddLine("{0}不能为0。", pptName);
                    if (value.ToString().IsMatch(@"\.") && !attNumber.AllowDecimal)
                        wrongMessage = wrongMessage.AddLine("{0}必须是整数。", pptName);
                    if (value<0 && !attNumber.AllowNegative)
                        wrongMessage = wrongMessage.AddLine("{0}不能为负数。", pptName);
                }
                if (attText.IsNotNull())
                {
                    string value = string.Empty;
                    if (pptValue.IsNotNull())
                        value = pptValue.ToString();
                    if (value.Length > attText.MaxLength)
                        wrongMessage = wrongMessage.AddLine("{0}超过最大限定字符数:{1}。", pptName, attText.MaxLength);
                    if (value.Length < attText.MinLength)
                        wrongMessage = wrongMessage.AddLine("{0}少于最小限定字符数:{1}。", pptName, attText.MinLength);
                    if (attText.ForFilePath && !value.CanUseToFileName())
                        wrongMessage = wrongMessage.AddLine("{0}中包含非法字符，导致其无法用于文件路径。", pptName);
                    if (attText.AllowEmpty && string.IsNullOrWhiteSpace(value))
                        wrongMessage = wrongMessage.AddLine("{0}不能为空。", pptName);
                    if (attText.AllowSpace && value.IsMatch(@"\s"))
                        wrongMessage = wrongMessage.AddLine("{0}不能包含空白字符。", pptName);
                    if (value.AllContains(attText.ProhibitedCharacters))
                        wrongMessage = wrongMessage.AddLine("{0}中含有限定的非法字符。", pptName);
                }
                if (attTime.IsNotNull())
                {
                    DateTime value = default;
                    try
                    {
                        value = Convert.ToDateTime(pptValue);
                    }
                    catch (Exception e) { wrongMessage = wrongMessage.AddLine("{0}错误：{1}", e.Message); }
                    if (value > attTime.LongestTime)
                        wrongMessage = wrongMessage.AddLine("{0}晚于限定最久日期:{1}。", pptName, attTime.LongestTime);
                    if (value < attTime.EarliesTime)
                        wrongMessage = wrongMessage.AddLine("{0}早于限定最早日期:{1}。", pptName, attTime.EarliesTime);
                }
            }
            return string.IsNullOrWhiteSpace(wrongMessage);
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NumberLimitAttribute : Attribute
    {
        public object DefaultValue { get; set; } = 0;
        public decimal MaxNumber { get; set; } = decimal.MaxValue;
        public decimal MinNumber { get; set; } = decimal.MinValue;
        public bool AllowZero { get; set; } = true;
        public bool AllowDecimal { get; set; } = true;
        public bool AllowNegative { get; set; } = true;
        public NumberLimitAttribute(decimal maxNumber)
        {
            this.MaxNumber = maxNumber;
        }
        public NumberLimitAttribute(bool allowNegative)
        {
            this.AllowNegative = allowNegative;
        }
        public NumberLimitAttribute(decimal maxNumber, bool allowNegative) : this(maxNumber)
        {
            this.AllowNegative = allowNegative;
        }
        public NumberLimitAttribute(decimal maxNumber,decimal minNumber):this(maxNumber)
        {
            this.MinNumber = minNumber;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class TextLimitAttribute : Attribute
    {
        public object DefaultValue { get; set; } = string.Empty;
        public decimal MaxLength { get; set; } = decimal.MaxValue;
        public decimal MinLength { get; set; } = decimal.MinValue;
        public bool ForFilePath { get; set; } = false;
        public bool AllowSpace { get; set; } = true;
        public bool AllowEmpty { get; set; } = true;
        public char[] ProhibitedCharacters { get; set; }
        public TextLimitAttribute(decimal maxLength)
        {
            this.MaxLength = maxLength;
        }
        public TextLimitAttribute(bool forFilePath)
        {
            this.ForFilePath = forFilePath;
        }
        public TextLimitAttribute(decimal maxLength , bool forFilePath):this(maxLength)
        {
            this.ForFilePath = forFilePath;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class TimeLimitAttribute : Attribute
    {
        public object DefaultValue { get; set; } = DateTime.Now;
        public DateTime LongestTime { get; } = DateTime.MaxValue;
        public DateTime EarliesTime { get; } = new DateTime(1900,1,1);
        public TimeLimitAttribute(long earliesDay)
        {
            this.EarliesTime = DateTime.Now.AddDays(-earliesDay);
        }
        public TimeLimitAttribute(long earliesDay, long longestDay) : this(earliesDay)
        {
            this.LongestTime = DateTime.Now.AddDays(longestDay);
        }
    }
}
