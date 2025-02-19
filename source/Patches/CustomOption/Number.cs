using System;
using UnityEngine;

namespace TownOfUs.CustomOption
{
    public class CustomNumberOption : CustomOption
    {
        protected internal CustomNumberOption(int id, MultiMenu menu, string name, float value, float min, float max, float increment,
            Func<object, string> format = null) : base(id, menu, name, CustomOptionType.Number, value, format)
        {
            Min = min;
            Max = max;
            Increment = increment;
        }

        protected float Min { get; set; }
        protected float Max { get; set; }
        protected float Increment { get; set; }

        protected internal float Get()
        {
            return (float)Value;
        }

        protected internal void Increase()
        {
            var increment = Increment > 5 && Input.GetKeyInt(KeyCode.LeftShift) ? 5 : Increment;

            if (Get() + increment > Max + 0.001f) // the slight increase is because of the stupid float rounding errors in the Giant speed
                Set(Min);
            else
                Set(Get() + increment);
        }

        protected internal void Decrease()
        {
            var increment = Increment > 5 && Input.GetKeyInt(KeyCode.LeftShift) ? 5 : Increment;

            if (Get() - increment < Min - 0.001f) // added it here to in case I missed something else
                Set(Max);
            else
                Set(Get() - increment);
        }

        public override void OptionCreated()
        {
            base.OptionCreated();
            var number = Setting.Cast<NumberOption>();
            number.ValidRange = new FloatRange(Min, Max);
            number.Increment = Increment;
            number.Value = number.oldValue = Get();
            number.ValueText.text = ToString();
        }
    }
}