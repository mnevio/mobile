﻿using System;
using System.Collections.Generic;


namespace SplitAndMerge
{
    public class UIVariable : Variable
    {
        public Action<string, string> ActionDelegate;

        public static List<UIVariable> WidgetTypes = new List<UIVariable>();

        protected static int m_currentTag;

        public enum UIType
        {
            NONE, LOCATION, VIEW, BUTTON, LABEL, TEXT_FIELD, TEXT_VIEW, EDIT_VIEW, PICKER_VIEW, PICKER_IMAGES,
            LIST_VIEW, COMBOBOX, IMAGE_VIEW, INDICATOR, SWITCH, SLIDER, STEPPER, SEGMENTED, CUSTOM
        };
        public UIVariable()
        {
            WidgetType = UIType.NONE;
        }
        public UIVariable(UIType type, string name = "",
                          UIVariable refViewX = null, UIVariable refViewY = null)
        {
            WidgetType = type;
            WidgetName = name;
            RefViewX = refViewX;
            RefViewY = refViewY;
        }

        public override Variable Clone()
        {
            UIVariable newVar = (UIVariable)this.MemberwiseClone();
            return newVar;
        }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
        public void SetRules(string ruleX, string ruleY)
        {
            RuleX = ruleX;
            RuleY = ruleY;
        }
        public string Name
        {
            get { return WidgetName; }
        }

        public UIType WidgetType { get; set; }
        public string WidgetName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int TranslationX { get; set; }
        public int TranslationY { get; set; }

        public string RuleX { get; set; }
        public string RuleY { get; set; }

        public UIVariable Location { get; set; }
        public UIVariable RefViewX { get; set; }
        public UIVariable RefViewY { get; set; }
        public UIVariable ParentView { get; set; }

        public Variable InitValue { get; set; }
        public int Alignment { get; set; }
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
        public double CurrVal { get; set; }
        public double Step { get; set; }

        public override string AsString(bool isList = true, bool sameLine = true, int maxCount = -1)
        {
            string baseStr = base.AsString(isList, sameLine, maxCount);
            if (!string.IsNullOrEmpty(baseStr))
            {
                return baseStr;
            }
            return WidgetName;
        }

        public override string GetTypeString()
        {
            switch (WidgetType)
            {
                case UIVariable.UIType.VIEW:
                    return "View";
                case UIVariable.UIType.BUTTON:
                    return "Button";
                case UIVariable.UIType.LABEL:
                    return "Label";
                case UIVariable.UIType.TEXT_FIELD:
                    return "TextEdit";
                case UIVariable.UIType.TEXT_VIEW:
                    return "TextView";
                case UIVariable.UIType.IMAGE_VIEW:
                    return "ImageView";
                case UIVariable.UIType.COMBOBOX:
                    return "Combobox";
                case UIVariable.UIType.INDICATOR:
                    return "Indicator";
                case UIVariable.UIType.PICKER_VIEW:
                    return "TypePicker";
                case UIVariable.UIType.PICKER_IMAGES:
                    return "Picker";
                case UIVariable.UIType.LIST_VIEW:
                    return "ListView";
                case UIVariable.UIType.SWITCH:
                    return "Switch";
                case UIVariable.UIType.SLIDER:
                    return "Slider";
                case UIVariable.UIType.STEPPER:
                    return "Stepper";
                case UIVariable.UIType.SEGMENTED:
                    return "SegmentedControl";
            }
            return Constants.TypeToString(Type);
        }

        public static Variable GetAction(string funcName, string senderName = null,
                               string eventArg1 = null, string eventArg2 = null)
        {
            var arg1 = senderName == null ? null : new Variable(senderName);
            var arg2 = eventArg1  == null ? null : new Variable(eventArg1);
            var arg3 = eventArg2  == null ? null : new Variable(eventArg2);
            System.Threading.Tasks.Task<Variable> task = null;
            try
            {
                task = CustomFunction.Run(funcName, arg1, arg2, arg3);
            }
            catch (Exception exc)
            {
                try
                {
                    task = CustomFunction.Run("OnException", new Variable(funcName),
                                              new Variable(exc.Message), arg2);
                }
                catch
                {
                }
            }
            return task == null ? Variable.EmptyInstance : task.Result;
        }
    }
}

