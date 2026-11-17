using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace StateCraft.UI
{
    public static class UIComponentLibrary
    {
        public enum SliderSize { Small, Medium, Large }

        public static VisualElement CreateProgressBar(float initialValue, float max, SliderSize size = SliderSize.Medium)
        {
            var bg = new VisualElement();
            bg.AddToClassList("sc-progress-bar-bg");
            
            switch (size)
            {
                case SliderSize.Small: bg.AddToClassList("sc-size-small"); break;
                case SliderSize.Medium: bg.AddToClassList("sc-size-medium"); break;
                case SliderSize.Large: bg.AddToClassList("sc-size-large"); break;
            }

            var fill = new VisualElement();
            fill.AddToClassList("sc-progress-bar-fill");
            bg.Add(fill);

            bg.userData = new Action<float>((val) => 
            {
                float pct = Mathf.Clamp01(val / max);
                fill.style.width = Length.Percent(pct * 100f);
                
                fill.RemoveFromClassList("sc-fill-low");
                fill.RemoveFromClassList("sc-fill-medium");
                fill.RemoveFromClassList("sc-fill-high");
                fill.RemoveFromClassList("sc-fill-full");

                if (pct <= 0.25f) fill.AddToClassList("sc-fill-low");
                else if (pct <= 0.50f) fill.AddToClassList("sc-fill-medium");
                else if (pct <= 0.75f) fill.AddToClassList("sc-fill-high");
                else fill.AddToClassList("sc-fill-full");
            });

            ((Action<float>)bg.userData).Invoke(initialValue);
            return bg;
        }

        public static void SetProgressBarValue(VisualElement bar, float value)
        {
            if (bar != null && bar.userData is Action<float> setter)
            {
                setter.Invoke(value);
            }
        }

        public static SliderInt CreateSliderInt(int min, int max, int current, Action<int> onChanged)
        {
            var slider = new SliderInt(min, max, SliderDirection.Horizontal) { value = current };
            slider.AddToClassList("sc-slider");
            
            if (onChanged != null)
            {
                slider.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            }
            return slider;
        }

        public static VisualElement CreateStepper(int currentVal, int step, int maxAllowed, Action<int> onValueChanged)
        {
            var container = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var valLbl = new Label(currentVal.ToString("N0")) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, minWidth = 60 } };
            
            var btnMinus = new Button(() => { 
                int n = Mathf.Max(0, currentVal - step); 
                currentVal = n;
                valLbl.text = currentVal.ToString("N0");
                onValueChanged?.Invoke(currentVal);
            }) { text = "-" };
            btnMinus.AddToClassList("sc-stepper-btn");
            
            var btnPlus = new Button(() => { 
                int n = Mathf.Min(maxAllowed, currentVal + step); 
                currentVal = n;
                valLbl.text = currentVal.ToString("N0");
                onValueChanged?.Invoke(currentVal);
            }) { text = "+" };
            btnPlus.AddToClassList("sc-stepper-btn");

            container.userData = new Action<int, int>((newVal, newMax) => {
                currentVal = newVal;
                maxAllowed = newMax;
                valLbl.text = currentVal.ToString("N0");
            });

            container.Add(btnMinus);
            container.Add(valLbl);
            container.Add(btnPlus);

            return container;
        }
        
        public static void UpdateStepper(VisualElement stepper, int newVal, int newMax)
        {
            if (stepper != null && stepper.userData is Action<int, int> setter)
            {
                setter.Invoke(newVal, newMax);
            }
        }
    }
}
