using System;
using System.ComponentModel;
using System.Reflection;

namespace XGame
{
    public class EnumUtils
    {
        /// <summary>
        /// 根据枚举值获取任务状态描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetQuestStatusDescription(byte value)
        {
            QuestStatusEnum questStatusEnum = (QuestStatusEnum)value;
            return GetDescription(questStatusEnum);
        }

        /// <summary>
        /// 根据枚举值获取任务类型描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetQuestTypeDescription(byte value)
        {
            QuestTypeEnum questTypeEnum = (QuestTypeEnum)value;
            return GetDescription(questTypeEnum);
        }

        /// <summary>
        /// 根据枚举值获取任务达成条件类型描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetQuestConditionTypeDescription(byte value)
        {
            QuestConditionTypeEnum conditionTypeEnum = (QuestConditionTypeEnum)value;
            return GetDescription(conditionTypeEnum);
        }
        
        /// <summary>
        /// 通用从枚举中获取描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetDescription(Enum value)
        
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            DescriptionAttribute attribute
                = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                    as DescriptionAttribute;

            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}