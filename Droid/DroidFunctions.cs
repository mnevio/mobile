﻿using System;
using System.Collections.Generic;
using System.IO;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using SplitAndMerge;

namespace scripting.Droid
{
  public class GetLocationFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 4, m_name);

      string viewNameX = args[0].AsString();
      string ruleStrX = args[1].AsString();
      string viewNameY = args[2].AsString();
      string ruleStrY = args[3].AsString();

      int leftMargin = Utils.GetSafeInt(args, 4);
      int topMargin = Utils.GetSafeInt(args, 5);

      string autoSize = Utils.GetSafeString(args, 6);
      double multiplier = Utils.GetSafeDouble(args, 7);
      AutoScaleFunction.TransformSizes(ref leftMargin, ref topMargin,
                        UtilsDroid.GetScreenSize().Width, autoSize, multiplier);

      Variable parentView = Utils.GetSafeVariable(args, 9, null);

      DroidVariable refViewX = viewNameX == "ROOT" ? null :
          Utils.GetVariable(viewNameX, script) as DroidVariable;

      DroidVariable refViewY = viewNameY == "ROOT" ? null :
          Utils.GetVariable(viewNameY, script) as DroidVariable;

      DroidVariable location = new DroidVariable(UIVariable.UIType.LOCATION, viewNameX + viewNameY,
                                                 refViewX, refViewY);

      location.ViewX = refViewX == null ? null : refViewX.ViewX;
      location.ViewY = refViewY == null ? null : refViewY.ViewX;

      location.SetRules(ruleStrX, ruleStrY);
      location.ParentView = parentView as DroidVariable;

      location.TranslationX = leftMargin;
      location.TranslationY = topMargin;
      return location;
    }
  }

  public class AddWidgetFunction : ParserFunction
  {
    public AddWidgetFunction(string widgetType = "", string extras = "")
    {
      m_widgetType = widgetType;
      m_extras = extras;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
      string widgetType = m_widgetType;
      int start = string.IsNullOrEmpty(widgetType) ? 1 : 0;
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2 + start, m_name);

      if (start == 1) {
        widgetType = args[0].AsString();
        Utils.CheckNotEmpty(script, widgetType, m_name);
      }
      DroidVariable location = args[start] as DroidVariable;
      Utils.CheckNotNull(location, m_name);

      string varName = args[start + 1].AsString();
      string text = Utils.GetSafeString(args, start + 2);
      int width = Utils.GetSafeInt(args, start + 3);
      int height = Utils.GetSafeInt(args, start + 4);

      string autoSize = Utils.GetSafeString(args, start + 5);
      double multiplier = Utils.GetSafeDouble(args, start + 6);
      AutoScaleFunction.TransformSizes(ref width, ref height,
                        UtilsDroid.GetScreenSize().Width, autoSize, multiplier);

      location.SetSize(width, height);
      location.LayoutRuleX = UtilsDroid.String2LayoutParam(location, true);
      location.LayoutRuleY = UtilsDroid.String2LayoutParam(location, false);

      DroidVariable widgetFunc = ExistingWidget(script, varName);
      if (widgetFunc == null) {
        widgetFunc = GetWidget(widgetType, varName, text, width, height);
      }

      Utils.CheckNotNull(widgetFunc, m_name);

      widgetFunc.Location = location;
      View widget = widgetFunc.ViewX;

      RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(
          ViewGroup.LayoutParams.WrapContent,
          ViewGroup.LayoutParams.WrapContent
      );

      View viewToAdd = widget;
      if (widgetFunc.WidgetType == UIVariable.UIType.VIEW) {
        widget = viewToAdd = widgetFunc.SetViewLayout(width, height);
        layoutParams = widgetFunc.ViewLayout.LayoutParameters
                                 as RelativeLayout.LayoutParams;
      } else if (widgetFunc.WidgetType == UIVariable.UIType.STEPPER) {
        widget = viewToAdd = widgetFunc.CreateStepper(width, height, m_extras);
        layoutParams = widgetFunc.ViewLayout.LayoutParameters
                                 as RelativeLayout.LayoutParams;
      }

      ApplyRule(layoutParams, location.LayoutRuleX, location.ViewX);
      ApplyRule(layoutParams, location.LayoutRuleY, location.ViewY);

      //layoutParams.SetMargins(location.LeftMargin,  location.TopMargin,
      //                        location.RightMargin, location.BottomMargin);
      if (location.Height > 0 || location.Width > 0) {
        layoutParams.Height = location.Height;
        layoutParams.Width = location.Width;
      }
      widget.LayoutParameters = layoutParams;

      widget.TranslationX = location.TranslationX;
      widget.TranslationY = location.TranslationY;

      var parentView = location.ParentView as DroidVariable;
      MainActivity.AddView(viewToAdd, parentView?.ViewLayout);

      ParserFunction.AddGlobal(varName, new GetVarFunction(widgetFunc));
      return widgetFunc;
    }

    public static DroidVariable GetWidget(string widgetType, string widgetName, string initArg,
                                         int width, int height)
    {
      UIVariable.UIType type = UIVariable.UIType.NONE;
      View widget = null;
      switch (widgetType) {
        case "View":
          type = UIVariable.UIType.VIEW;
          widget = new View(MainActivity.TheView);
          break;
        case "Button":
          type = UIVariable.UIType.BUTTON;
          widget = new Button(MainActivity.TheView);
          ((Button)widget).SetTextColor(Color.Black);
          ((Button)widget).Text = initArg;
          UtilsDroid.AddViewBorder(widget, Color.Black);
          break;
        case "Label":
          type = UIVariable.UIType.LABEL;
          widget = new TextView(MainActivity.TheView);
          ((TextView)widget).SetTextColor(Color.Black);
          ((TextView)widget).Text = initArg;
          break;
        case "TextView":
        case "TextEdit":
          type = UIVariable.UIType.TEXT_FIELD;
          widget = new EditText(MainActivity.TheView);
          ((EditText)widget).SetTextColor(Color.Black);
          ((EditText)widget).Hint = initArg;
          break;
        case "ImageView":
          type = UIVariable.UIType.IMAGE_VIEW;
          widget = new ImageView(MainActivity.TheView);
          if (!string.IsNullOrWhiteSpace(initArg)) {
            int resourceID = MainActivity.String2Pic(initArg);
            widget.SetBackgroundResource(resourceID);
          }
          break;
        case "Combobox":
          type = UIVariable.UIType.COMBOBOX;
          widget = new Spinner(MainActivity.TheView);
          //((Spinner)widget).Prompt = "Select Language";
          ((Spinner)widget).DescendantFocusability = DescendantFocusability.BlockDescendants;
          break;
        case "TypePicker":
          type = UIVariable.UIType.PICKER_VIEW;
          widget = new NumberPicker(MainActivity.TheView);
          // Don't show the cursor on the picker:
          ((NumberPicker)widget).DescendantFocusability = DescendantFocusability.BlockDescendants;
          break;
        case "ListView":
          type = UIVariable.UIType.LIST_VIEW;
          widget = new ListView(MainActivity.TheView);
          // Don't show the cursor on the list view:
          ((ListView)widget).DescendantFocusability = DescendantFocusability.BlockDescendants;
          break;
        case "Switch":
          type = UIVariable.UIType.SWITCH;
          widget = new Switch(MainActivity.TheView);
          break;
        case "SegmentedControl":
          type = UIVariable.UIType.SEGMENTED;
          widget = new Switch(MainActivity.TheView);
          break;
        case "Slider":
          type = UIVariable.UIType.SLIDER;
          widget = new SeekBar(MainActivity.TheView);
          break;
        case "Stepper":
          type = UIVariable.UIType.STEPPER;
          widget = new View(MainActivity.TheView);
          break;
        case "AdMobBanner":
          type = UIVariable.UIType.ADMOB;
          widget = AdMob.AddBanner(MainActivity.TheView, initArg);
          break;
        default:
          type = UIVariable.UIType.VIEW;
          widget = new View(MainActivity.TheView);
          break;
      }

      DroidVariable widgetFunc = new DroidVariable(type, widgetName, widget);
      SetValues(widgetFunc, initArg);
      return widgetFunc;
    }

    public static void SetValues(DroidVariable widgetFunc, string valueStr)
    {
      if (string.IsNullOrWhiteSpace(valueStr)) {
        return;
      }
      widgetFunc.InitValue = new Variable(valueStr);

      // currValue:minValue:maxValue:step

      double minValue = 0, maxValue = 1, currValue = 0, step = 1.0;
      string[] vals = valueStr.Split(new char[] { ',', ':' });
      Double.TryParse(vals[0].Trim(), out currValue);

      if (vals.Length > 1) {
        Double.TryParse(vals[1].Trim(), out minValue);
        if (vals.Length > 2) {
          Double.TryParse(vals[2].Trim(), out maxValue);
        }
        if (vals.Length > 3) {
          Double.TryParse(vals[3].Trim(), out step);
        }
      } else {
        minValue = maxValue = currValue;
      }

      if (widgetFunc.WidgetType == UIVariable.UIType.SEGMENTED) {
        Switch seg = widgetFunc.ViewX as Switch;
        seg.ShowText = true;
        seg.TextOn = vals[vals.Length - 1];
        seg.TextOff = vals[0];
        seg.Checked = false;
      } else if (widgetFunc.ViewX is Switch) {
        Switch sw = widgetFunc.ViewX as Switch;
        sw.Checked = (int)currValue == 1;
      } else if (widgetFunc.ViewX is SeekBar) {
        SeekBar slider = widgetFunc.ViewX as SeekBar;
        slider.Max = (int)maxValue - (int)minValue;
        slider.Progress = (int)currValue;
        widgetFunc.MinVal = minValue;
        widgetFunc.MaxVal = maxValue;
        widgetFunc.CurrVal = currValue;
      } else {
        widgetFunc.MinVal = minValue;
        widgetFunc.MaxVal = maxValue;
        widgetFunc.CurrVal = currValue;
        widgetFunc.Step = step;
      }
    }
    public static void ApplyRule(RelativeLayout.LayoutParams layoutParams,
                                 LayoutRules rule, View view = null)
    {
      if (view != null) {
        layoutParams.AddRule(rule, view.Id);
      } else {
        layoutParams.AddRule(rule);
      }
    }
    public static DroidVariable ExistingWidget(ParsingScript script, string varName)
    {
      ParserFunction func = ParserFunction.GetFunction(varName);
      if (func == null) {
        return null;
      }
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      RemoveViewFunction.RemoveView(viewVar);
      return viewVar;
    }

    string m_widgetType;
    string m_extras;
  }
  public class AddBorderFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 1, m_name);
      DroidVariable viewVar = args[0] as DroidVariable;
      View view = viewVar.ViewX;

      Utils.CheckNotNull(view, m_name);

      int width = Utils.GetSafeInt(args, 1, 1);
      int corner = Utils.GetSafeInt(args, 2, 5);
      string colorStr = Utils.GetSafeString(args, 3, "#000000");
      Color color = Color.ParseColor(colorStr);

      UtilsDroid.AddViewBorder(view, color, width, corner);
      return Variable.EmptyInstance;
    }
  }

  public class AddTabFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string text = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, text, m_name);

      string imageName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, text, m_name);

      string selectedImageName = null;
      if (script.Current == Constants.NEXT_ARG) {
        selectedImageName = Utils.GetItem(script).AsString();
      }

      MainActivity.AddTab(text, imageName, selectedImageName);

      return Variable.EmptyInstance;
    }
  }
  public class AddWidgetDataFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      ParserFunction func = ParserFunction.GetFunction(varName);
      Utils.CheckNotNull(func, varName);
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      Utils.CheckNotNull(viewVar, m_name);

      Variable data = Utils.GetItem(script);
      Utils.CheckNotNull(data.Tuple, m_name);

      List<string> types = new List<string>(data.Tuple.Count);
      for (int i = 0; i < data.Tuple.Count; i++) {
        types.Add(data.Tuple[i].AsString());
      }

      Variable actionValue = Utils.GetItem(script);
      string strAction = actionValue.AsString();
      script.MoveForwardIf(Constants.NEXT_ARG);

      // Not used at the moment:
      string alignment = Utils.GetItem(script).AsString();

      if (viewVar.ViewX is NumberPicker) {
        NumberPicker pickerView = viewVar.ViewX as NumberPicker;

        pickerView.SaveFromParentEnabled = false;
        pickerView.SaveEnabled = true;

        pickerView.SetDisplayedValues(types.ToArray());
        pickerView.MinValue = 0;
        pickerView.MaxValue = types.Count - 1;
        pickerView.Value = 0;
        pickerView.WrapSelectorWheel = false;

        AddActionFunction.AddAction(viewVar, varName, strAction);
      } else if (viewVar.ViewX is Spinner) {
        Spinner spinner = viewVar.ViewX as Spinner;
        var adapter = spinner.Adapter as TextImageAdapter;
        if (adapter == null) {
          adapter = new TextImageAdapter(MainActivity.TheView);
        }
        string first = viewVar.InitValue == null ? null : viewVar.InitValue.AsString();
        adapter.SetItems(types, first);
        spinner.Adapter = adapter;
        AddActionFunction.AddAction(viewVar, varName, strAction);
      } else if (viewVar.ViewX is ListView) {
        ListView listView = viewVar.ViewX as ListView;
        var adapter = listView.Adapter as TextImageAdapter;
        if (adapter == null) {
          adapter = new TextImageAdapter(MainActivity.TheView);
        }
        adapter.SetItems(types);
        listView.Adapter = adapter;
        AddActionFunction.AddAction(viewVar, varName, strAction);
      }

      return Variable.EmptyInstance;
    }
  }
  public class AddWidgetImagesFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      ParserFunction func = ParserFunction.GetFunction(varName);
      Utils.CheckNotNull(func, varName);
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      Utils.CheckNotNull(viewVar, m_name);

      Variable data = Utils.GetItem(script);
      Utils.CheckNotNull(data.Tuple, m_name);

      List<string> images = new List<string>(data.Tuple.Count);
      for (int i = 0; i < data.Tuple.Count; i++) {
        images.Add(data.Tuple[i].AsString());
      }

      Variable actionValue = Utils.GetItem(script);
      string strAction = actionValue.AsString();
      script.MoveForwardIf(Constants.NEXT_ARG);

      if (viewVar.ViewX is Spinner) {
        Spinner spinner = viewVar.ViewX as Spinner;
        var adapter = spinner.Adapter as TextImageAdapter;
        if (adapter == null) {
          adapter = new TextImageAdapter(MainActivity.TheView);
        }
        adapter.SetPics(images);
        spinner.Adapter = adapter;
        if (!string.IsNullOrEmpty(strAction)) {
          AddActionFunction.AddAction(viewVar, varName, strAction);
        }
      } else if (viewVar.ViewX is ListView) {
        ListView listView = viewVar.ViewX as ListView;
        var adapter = listView.Adapter as TextImageAdapter;
        if (adapter == null) {
          adapter = new TextImageAdapter(MainActivity.TheView);
        }
        adapter.SetPics(images);
        listView.Adapter = adapter;
        if (!string.IsNullOrEmpty(strAction)) {
          AddActionFunction.AddAction(viewVar, varName, strAction);
        }
      }

      return Variable.EmptyInstance;
    }
  }
  public class MoveFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 3, m_name);

      string varName = args[0].AsString();
      ParserFunction func = ParserFunction.GetFunction(varName);
      Utils.CheckNotNull(func, varName);
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      Utils.CheckNotNull(viewVar, m_name);

      int deltaX = args[1].AsInt();
      int deltaY = args[2].AsInt();

      string autoSize = Utils.GetSafeString(args, 3);
      double multiplier = Utils.GetSafeDouble(args, 4);
      AutoScaleFunction.TransformSizes(ref deltaX, ref deltaY,
                        UtilsDroid.GetScreenSize().Width, autoSize, multiplier);

      View view = viewVar.ViewX;
      Utils.CheckNotNull(view, m_name);

      view.SetX(view.GetX() + deltaX);
      view.SetY(view.GetY() + deltaY);
      //view.TranslationX = deltaX;
      //view.TranslationY = deltaY;

      return Variable.EmptyInstance;
    }
  }
  public class ShowHideFunction : ParserFunction
  {
    bool m_show;
    public ShowHideFunction(bool show)
    {
      m_show = show;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 1, m_name);

      string varName = Utils.GetSafeString(args, 0);
      Utils.CheckNotEmpty(script, varName, m_name);

      bool show = Utils.GetSafeInt(args, 1, m_show ? 1 : 0) != 0;

      ParserFunction func = ParserFunction.GetFunction(varName);
      Utils.CheckNotNull(func, varName);
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      Utils.CheckNotNull(viewVar, m_name);

      // Special dealing if the user tries to show/hide the layout:
      View view = viewVar.ViewX;
      if (view == null) {
        // Otherwise it's a a Main Root view.
        view = MainActivity.TheLayout.RootView;
      }

      MainActivity.ShowView(view, show);

      return Variable.EmptyInstance;
    }
  }
  public class RemoveViewFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      ParserFunction func = ParserFunction.GetFunction(varName);
      Utils.CheckNotNull(func, varName);
      DroidVariable viewVar = func.GetValue(script) as DroidVariable;
      Utils.CheckNotNull(viewVar, m_name);

      RemoveView(viewVar);
            return Variable.EmptyInstance;
    }
    public static void RemoveView(DroidVariable viewVar)
    {
      if (viewVar == null || viewVar.ViewX == null) {
        return;
      }
      var parent = viewVar.Location?.ParentView as DroidVariable;

      ViewGroup parentView = parent != null ? parent.ViewLayout : MainActivity.TheLayout;
      View viewToRemove = viewVar.ViewX;

      parentView.RemoveView(viewToRemove);
    }
  }
  public class RemoveAllViewsFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      MainActivity.RemoveAll();

      return Variable.EmptyInstance;
    }
  }
  public class GetSelectedTabFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      int tabId = MainActivity.CurrentTabId;
      script.MoveForwardIf(Constants.END_ARG);
      return new Variable(tabId);
    }
  }
  public class SelectTabFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      int tabId = Utils.GetItem(script).AsInt();
      MainActivity.TheView.ChangeTab(tabId);
      return Variable.EmptyInstance;
    }
  }
  public class SetSizeFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 3, m_name);
      string varName = Utils.GetSafeString(args, 0);
      Utils.CheckNotEmpty(script, varName, m_name);

      Variable width = Utils.GetSafeVariable(args, 1);
      Utils.CheckNonNegativeInt(width);

      Variable height = Utils.GetSafeVariable(args, 2);
      Utils.CheckNonNegativeInt(height);

      View view = DroidVariable.GetView(varName, script);

      var layoutParams = view.LayoutParameters;
      if (width.AsInt() > 0) {
        layoutParams.Width = width.AsInt();
      }
      if (height.AsInt() > 0) {
        layoutParams.Height = height.AsInt();
      }
      view.LayoutParameters = layoutParams;

      return Variable.EmptyInstance;
    }
  }
  public class SetImageFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      Variable imageNameVar = Utils.GetItem(script);
      string imageStr = imageNameVar.AsString();

      string imageName = UIUtils.String2ImageName(imageStr);

      int resourceID = MainActivity.String2Pic(imageName);

      if (resourceID > 0) {
        View view = DroidVariable.GetView(varName, script);
        view.SetBackgroundResource(resourceID);
      } else {
        Console.WriteLine("Couldn't find pic [{0}]", imageName);
      }

      return resourceID > 0 ? new Variable(imageName) : Variable.EmptyInstance;
    }
  }
  public class SetBackgroundColorFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      Variable color = Utils.GetItem(script);
      string strColor = color.AsString();

      View view = DroidVariable.GetView(varName, script);
      if (view == null) {
        view = MainActivity.TheLayout.RootView;
      }
      view.SetBackgroundColor(String2Color(strColor));

      return Variable.EmptyInstance;
    }
    public static Color String2Color(string colorStr)
    {
      switch (colorStr) {
        case "blue": return Color.Blue;
        case "cyan": return Color.Cyan;
        case "green": return Color.Green;
        case "yellow": return Color.Yellow;
        case "white": return Color.White;
        case "red": return Color.Red;
        case "brown": return Color.Brown;
        case "orange": return Color.Orange;
        case "rose": return Color.Magenta;
        case "gray": return Color.LightGray;
        case "purple": return Color.Purple;
        default: return Color.Transparent;
      }
    }
  }

  public class SetBackgroundImageFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      Variable imageNameVar = Utils.GetItem(script);
      string imageName = imageNameVar.AsString();

      int resourceID = MainActivity.String2Pic(imageName);

      View view = MainActivity.TheLayout.RootView;
      view.SetBackgroundResource(resourceID);

      return Variable.EmptyInstance;
    }
  }

  public class AddActionFunction : ParserFunction
  {
    static Dictionary<string, Tuple<string, string>> m_actions =
       new Dictionary<string, Tuple<string, string>>();
    
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 2, m_name);

      string varName   = Utils.GetSafeString(args, 0);
      string strAction = Utils.GetSafeString(args, 1);
      string argument  = Utils.GetSafeString(args, 2);

      DroidVariable droidVar = Utils.GetVariable(varName, script) as DroidVariable;
      Utils.CheckNotNull(droidVar, m_name);
      AddAction(droidVar, varName, strAction, argument);

      return Variable.EmptyInstance;
    }
    public static void AddAction(DroidVariable droidVar, string varName,
                                 string strAction, string argument = null)
    {
      if (string.IsNullOrWhiteSpace(strAction)) {
        return;
      }
      if (droidVar.ViewX is Button) {
        Button button = droidVar.ViewX as Button;
        button.Click += (sender, e) => {
          UIVariable.GetAction(strAction, varName, "\"" + argument + "\"");
        };
      } else if (droidVar.ViewX is EditText) {
        if (argument.Equals("FINISHED")) {
        } else {
          EditText editText = droidVar.ViewX as EditText;
          editText.TextChanged += (sender, e) => {
            UIVariable.GetAction(strAction, varName, "\"" + e.Text.ToString() + "\"");
          };
        }
      } else if (droidVar.ViewX is Switch) {
        Switch sw = droidVar.ViewX as Switch;
        sw.CheckedChange += (sender, e) => {
          UIVariable.GetAction(strAction, varName, "\"" + e + "\"");
        };
      } else if (droidVar.ViewX is SeekBar) {
        SeekBar slider = droidVar.ViewX as SeekBar;
        slider.ProgressChanged += (sender, e) => {
          UIVariable.GetAction(strAction, varName, "\"" + e + "\"");
        };
      } else if (droidVar.ViewX is NumberPicker) {
        NumberPicker pickerView = droidVar.ViewX as NumberPicker;
        pickerView.ValueChanged += (sender, e) => {
          UIVariable.GetAction(strAction, varName, e.NewVal.ToString());
        };
      } else if (droidVar.ViewX is Spinner) {
        Spinner spinner = droidVar.ViewX as Spinner;
        spinner.ItemSelected += (sender, e) => {
          UIVariable.GetAction(strAction, varName, e.Position.ToString());
        };
      } else if (droidVar.ViewX is ListView) {
        ListView listView = droidVar.ViewX as ListView;
        listView.ItemClick += (sender, e) => {
          UIVariable.GetAction(strAction, varName, e.Position.ToString());
        };
      } else {
        droidVar.ActionDelegate += (arg1, arg2) => {
          UIVariable.GetAction(strAction, varName, droidVar.CurrVal.ToString());
        };
      }

      m_actions[droidVar.Name] = new Tuple<string, string>(strAction, varName);
    }
    public static void ExecuteAction(DroidVariable droidVar, string arg)
    {
      Tuple<string, string> action;
      if (m_actions.TryGetValue(droidVar.Name, out action)) {
        UIVariable.GetAction(action.Item1, action.Item2, "\"" + arg + "\"");
      }
    }
  }
  public class AddLongClickFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      //string varName = Utils.GetToken(script, Constants.NEXT_ARG_ARRAY);
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);
      script.MoveForwardIf(Constants.NEXT_ARG);

      Variable actionValue = Utils.GetItem(script);
      string strAction = actionValue.AsString();
      script.MoveForwardIf(Constants.NEXT_ARG);

      View view = DroidVariable.GetView(varName, script);

      view.LongClick += (sender, e) => {
        UIVariable.GetAction(strAction, varName, "\"" + e + "\"");
      };

      return Variable.EmptyInstance;
    }

    /*public class GestureListener : Java.Lang.Object, View.IOnLongClickListener
    {
       public bool OnLongClick(View v)
       {
            return true;
       }
    }*/
  }

  public class AddSwipeFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 1, m_name);
      string varName = Utils.GetSafeString(args, 0);
      string direction = Utils.GetSafeString(args, 1);
      string strAction = Utils.GetSafeString(args, 2);

      View view = DroidVariable.GetView(varName, script);
      Utils.CheckNotNull(view, m_name);

      OnTouchListener.Direction dir = OnTouchListener.Direction.Left;
      switch (direction) {
        case "Left": dir = OnTouchListener.Direction.Left; break;
        case "Right": dir = OnTouchListener.Direction.Right; break;
        case "Down": dir = OnTouchListener.Direction.Down; break;
        case "Up": dir = OnTouchListener.Direction.Up; break;
      }

      OnTouchListener listener = new OnTouchListener(view, dir, strAction, varName);
      view.Touch += listener.OnTouch;

      return Variable.EmptyInstance;
    }
    public class OnTouchListener
    {
      public enum Direction { Left, Right, Down, Up }
      Direction m_direction;
      View m_view;
      string m_action;
      string m_varName;

      float m_startX;
      float m_startY;

      const float TOUCH_DELTA = 10;

      public OnTouchListener(View view, Direction dir,
                             string strAction = null, string varName = null)
      {
        m_direction = dir;
        m_view = view;
        m_action = strAction;
        m_varName = varName;
      }
      public void OnTouch(object sender, View.TouchEventArgs e)
      {
        if (e.Event.Action == MotionEventActions.Down) {
          m_startX = e.Event.GetX();
          m_startY = e.Event.GetY();
        } else if (e.Event.Action == MotionEventActions.Up) {
          float deltaX = e.Event.GetX() - m_startX;
          float deltaY = e.Event.GetY() - m_startY;

          if (m_direction == Direction.Left && deltaX < -1 * TOUCH_DELTA ||
              m_direction == Direction.Right && deltaX > TOUCH_DELTA ||
              m_direction == Direction.Up && deltaY < -1 * TOUCH_DELTA ||
              m_direction == Direction.Down && deltaY > TOUCH_DELTA) {
            triggerAction(deltaX, deltaY);
          }
        }
      }
      void triggerAction(float deltaX, float deltaY)
      {
        if (!string.IsNullOrWhiteSpace(m_action)) {
          UIVariable.GetAction(m_action, m_varName, "\"" + m_direction + "\"");
        }
      }
    }
  }
  public class AddDragAndDropFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 1, m_name);
      string varName = Utils.GetSafeString(args, 0);
      //string strAction = Utils.GetSafeString(args, 1);

      View view = DroidVariable.GetView(varName, script);
      Utils.CheckNotNull(view, m_name);

      OnTouchListener listener = new OnTouchListener(view);
      view.Touch += listener.OnTouch;

      /*DragEventListener listener = new DragEventListener(view);
      view.SetOnDragListener(listener);
      view.LongClick += (sender, e) => {
          ClipData data = ClipData.NewPlainText("x", "X");
          View.DragShadowBuilder shadowBuilder = new View.DragShadowBuilder(view);
          view.StartDrag(data, shadowBuilder, null, 0);
      };*/

      return Variable.EmptyInstance;
    }
    protected class OnTouchListener
    {
      View m_view;
      float m_startX;
      float m_startY;

      const float TOUCH_DELTA = 5;

      public OnTouchListener(View view)
      {
        m_view = view;
      }
      public void OnTouch(object sender, View.TouchEventArgs e)
      {
        if (e.Event.Action == MotionEventActions.Down) {
          m_startX = e.Event.GetX();
          m_startY = e.Event.GetY();
        } else {
          float deltaX = e.Event.GetX() - m_startX;
          float deltaY = e.Event.GetY() - m_startY;
          if (Math.Abs(deltaX) > TOUCH_DELTA || Math.Abs(deltaY) > TOUCH_DELTA) {
            //Console.WriteLine(e.Event.Action + ": " + deltaX + ", " + deltaY);
            m_view.SetX(m_view.GetX() + deltaX);
            m_view.SetY(m_view.GetY() + deltaY);

            m_startX = e.Event.GetX();
            m_startY = e.Event.GetY();
          }
        }
      }
    }
    // Not used, needs more tweaks.
    protected class DragEventListener : Java.Lang.Object, View.IOnDragListener
    {
      View m_view;
      float m_endX;
      float m_endY;
      bool m_started;

      public DragEventListener(View view)
      {
        m_view = view;
      }
      public bool OnDrag(View view, DragEvent ev)
      {
        DragAction action = ev.Action;
        bool handled = false;

        if (ev.GetX() != 0 || ev.GetY() != 0) {
          m_endX = ev.GetX();
          m_endY = ev.GetY();
          Console.WriteLine("{0}: {1} {2} -- {3} {4}", action, m_endX, m_endY,
                             m_view.GetX(), m_view.GetY());
        }
        m_view.Visibility = ViewStates.Visible;
        switch (action) {
          case DragAction.Started:
            if (ev.ClipDescription.HasMimeType(ClipDescription.MimetypeTextPlain)) {
              handled = true;
            }
            break;
          case DragAction.Entered:
          case DragAction.Ended:
            handled = true;
            break;
          case DragAction.Exited:
            handled = true;
            break;
          case DragAction.Drop:
            handled = true;
            break;
          case DragAction.Location:
            handled = true;
            break;
        }

        if (handled) {
          m_view.Visibility = ViewStates.Visible;
          m_view.SetX(m_endX);
          m_view.SetY(m_endY);
          view.Invalidate();
        } else if (!m_started) {
          view.Visibility = ViewStates.Invisible;
          m_started = true;
        }

        return handled;
      }
    }
  }
  public class SetTextFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      Variable title = Utils.GetItem(script);
      string text = title.AsString();

      DroidVariable droidVar = Utils.GetVariable(varName, script) as DroidVariable;
      Utils.CheckNotNull(droidVar, m_name);

      SetText(droidVar, text);

      return title;
    }
    public static void SetText(DroidVariable droidVar, string text)
    {
      if (droidVar.ViewX is Button) {
        ((Button)droidVar.ViewX).Text = text;
      } else if (droidVar.ViewX is TextView) {
        ((TextView)droidVar.ViewX).Text = text;
      } else if (droidVar.ViewX is EditText) {
        ((EditText)droidVar.ViewX).Text = text;
      } else if (droidVar.ViewX is Spinner) {
        Spinner spinner = droidVar.ViewX as Spinner;
        TextImageAdapter adapter = spinner.Adapter as TextImageAdapter;
        if (adapter != null) {
          int pos = adapter.Text2Position(text);
          spinner.SetSelection(pos);
        }
      } else if (droidVar.ViewX is NumberPicker) {
        NumberPicker picker = droidVar.ViewX as NumberPicker;
        string[] all = picker.GetDisplayedValues();
        List<string> names = new List<string>(all);
        int row = names.FindIndex((obj) => obj.Equals(text));
        SetValueFunction.SetValue(droidVar, row);
      }
    }
  }
  public class GetTextFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      DroidVariable droidVar = Utils.GetVariable(varName, script) as DroidVariable;
      Utils.CheckNotNull(droidVar, m_name);

      string text = GetText(droidVar);

      return new Variable(text);
    }
    public static string GetText(DroidVariable droidVar)
    {
      string text = "";
      if (droidVar.ViewX is Button) {
        text = ((Button)droidVar.ViewX).Text;
      } else if (droidVar.ViewX is TextView) {
        text = ((TextView)droidVar.ViewX).Text;
      } else if (droidVar.ViewX is EditText) {
        text = ((EditText)droidVar.ViewX).Text;
      } else if (droidVar.ViewX is Spinner) {
        Spinner spinner = droidVar.ViewX as Spinner;
        TextImageAdapter adapter = spinner.Adapter as TextImageAdapter;
        if (adapter != null) {
          int pos = spinner.SelectedItemPosition;
          text = adapter.Position2Text(pos);
        }
      } else if (droidVar.ViewX is NumberPicker) {
        NumberPicker picker = droidVar.ViewX as NumberPicker;
        string[] all = picker.GetDisplayedValues();
        int row = (int)GetValueFunction.GetValue(droidVar);
        if (all.Length > row && row >= 0) {
          text = all[row];
        }
      }
      return text;
    }
  }
  public class SetValueFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      Variable arg = Utils.GetItem(script);

      DroidVariable droidVar = Utils.GetVariable(varName, script) as DroidVariable;
      Utils.CheckNotNull(droidVar, m_name);

      SetValue(droidVar, arg.AsDouble());

      return droidVar;
    }
    public static void SetValue(DroidVariable droidVar, double val)
    {
      if (droidVar.ViewX is Switch) {
        ((Switch)droidVar.ViewX).Checked = (int)val == 1;
      } else if (droidVar.ViewX is SeekBar) {
        ((SeekBar)droidVar.ViewX).Progress = (int)val;
      } else if (droidVar.WidgetType == UIVariable.UIType.STEPPER) {
        droidVar.CurrVal = val;
      } else if (droidVar.ViewX is NumberPicker) {
        NumberPicker picker = droidVar.ViewX as NumberPicker;
        picker.Value = (int)val;
        AddActionFunction.ExecuteAction(droidVar, val.ToString());
      } else if (droidVar.WidgetType == UIVariable.UIType.SEGMENTED) {
        Switch sw = ((Switch)droidVar.ViewX);
        sw.Checked = val == 0;
      } else if (droidVar.ViewX is Spinner) {
        Spinner spinner = ((Spinner)droidVar.ViewX);
        spinner.SetSelection((int)val);
      }
    }
  }
  public class GetValueFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      DroidVariable droidVar = Utils.GetVariable(varName, script) as DroidVariable;
      Utils.CheckNotNull(droidVar, m_name);

      double result = GetValue(droidVar);

      return new Variable(result);
    }
    public static double GetValue(DroidVariable droidVar)
    {
      double result = 0;
      if (droidVar.ViewX is Switch) {
        result = ((Switch)droidVar.ViewX).Checked ? 1 : 0;
      } else if (droidVar.ViewX is SeekBar) {
        result = ((SeekBar)droidVar.ViewX).Progress;
      } else if (droidVar.WidgetType == UIVariable.UIType.STEPPER) {
        result = droidVar.CurrVal;
      } else if (droidVar.ViewX is NumberPicker) {
        result = ((NumberPicker)droidVar.ViewX).Value;
      } else if (droidVar.WidgetType == UIVariable.UIType.SEGMENTED) {
        Switch sw = ((Switch)droidVar.ViewX);
        result = sw.Checked ? 0 : 1;
      } else if (droidVar.ViewX is Spinner) {
        Spinner spinner = ((Spinner)droidVar.ViewX);
        result = spinner.SelectedItemPosition;
      }
      return result;
    }
  }

  public class AlignTitleFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);

      View view = DroidVariable.GetView(varName, script);
      Utils.CheckNotNull(view, m_name);

      string alignment = Utils.GetItem(script).AsString();
      alignment = alignment.ToLower();

      view.TextAlignment = TextAlignment.Gravity;
      GravityFlags gravity = GravityFlags.Center;

      switch (alignment) {
        case "left":
          gravity = GravityFlags.Left;
          break;
        case "right":
          gravity = GravityFlags.Right;
          break;
        case "fill":
        case "natural":
          gravity = GravityFlags.Fill;
          break;
        case "bottom":
          gravity = GravityFlags.Bottom;
          break;
        case "center":
          gravity = GravityFlags.Center;
          break;
        case "justified":
          gravity = GravityFlags.ClipHorizontal;
          break;
      }

      if (view is Button) {
        ((Button)view).Gravity = gravity;
      } else if (view is TextView) {
        ((TextView)view).Gravity = gravity;
      }

      return Variable.EmptyInstance;
    }
  }
  public class SetFontSizeFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string varName = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, varName, m_name);
      script.MoveForwardIf(Constants.NEXT_ARG);

      Variable fontSize = Utils.GetItem(script);
      Utils.CheckPosInt(fontSize);

      View view = DroidVariable.GetView(varName, script);
      Utils.CheckNotNull(view, m_name);

      if (view is Button) {
        ((Button)view).TextSize = (int)fontSize.Value;
      } else if (view is TextView) {
        ((TextView)view).TextSize = (int)fontSize.Value;
      } else if (view is EditText) {
        ((EditText)view).TextSize = (int)fontSize.Value;
      } else {
        return Variable.EmptyInstance;
      }

      return fontSize;
    }
  }
  public class SetFontColorFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2, m_name);

      string varName = args[0].AsString();
      View view = DroidVariable.GetView(varName, script);
      Utils.CheckNotNull(view, m_name);

      string colorStr = args[1].AsString();
      Color color = SetBackgroundColorFunction.String2Color(colorStr);

      if (view is Button) {
        ((Button)view).SetTextColor(color);
      } else if (view is TextView) {
        ((TextView)view).SetTextColor(color);
      } else if (view is EditText) {
        ((EditText)view).SetTextColor(color);
      } else {
        return Variable.EmptyInstance;
      }

      return Variable.EmptyInstance;
    }
  }

  public class GadgetSizeFunction : ParserFunction
  {
    bool m_needWidth;
    public GadgetSizeFunction(bool needWidth = true)
    {
      m_needWidth = needWidth;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
      DisplayMetrics bounds = new DisplayMetrics();
      var winManager = MainActivity.TheView.WindowManager;
      winManager.DefaultDisplay.GetMetrics(bounds);

      int width = bounds.WidthPixels < bounds.HeightPixels ?
                   bounds.WidthPixels : bounds.HeightPixels;
      int height = bounds.WidthPixels < bounds.HeightPixels ?
                   bounds.HeightPixels : bounds.WidthPixels;

      return new Variable(m_needWidth ? width : height);
    }
  }

  public class OrientationChangeFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      Variable actionValue = Utils.GetItem(script);
      string strAction = actionValue.AsString();
      Utils.CheckNotEmpty(script, strAction, m_name);

      MainActivity.OnOrientationChange += (newOrientation) => {
        UIVariable.GetAction(strAction, "\"ROOT\"", "\"" + newOrientation + "\"");
      };

      return Variable.EmptyInstance;
    }
  }

  public class OrientationFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      var or = MainActivity.TheView.Resources.Configuration.Orientation.ToString();

      return new Variable(or);
    }
  }

  public class ShowToastFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 1, m_name);
      string msg = Utils.GetSafeString(args, 0);
      int duration = Utils.GetSafeInt(args, 1, 10);
      string fgColorStr = Utils.GetSafeString(args, 2);
      string bgColorStr = Utils.GetSafeString(args, 3);

      Toast toast = Toast.MakeText(MainActivity.TheView, msg,
                                   duration < 3 ? ToastLength.Short : ToastLength.Long);

      if (args.Count > 2) {
        if (string.IsNullOrEmpty(fgColorStr)) {
          fgColorStr = "#FFFFFF";
        }
        if (string.IsNullOrEmpty(bgColorStr)) {
          bgColorStr = "#D3D3D3";
        }
        Color fgColor = Color.ParseColor(fgColorStr);
        Color bgColor = Color.ParseColor(bgColorStr);

        GradientDrawable shape = new GradientDrawable();
        shape.SetCornerRadius(20);
        shape.SetColor(bgColor);

        View toastView = toast.View;
        TextView toastText = (TextView)toastView.FindViewById(Android.Resource.Id.Message);
        toastText.SetTextColor(fgColor);
        toastView.Background = shape;
      }

      int shown = 0;
      while (shown < 2 * duration) {
        toast.Show();
        shown += 4;
      }

      return Variable.EmptyInstance;
    }
  }
  public class AlertDialogFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
          Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.CheckArgs(args.Count, 2, m_name);
      string title = Utils.GetSafeString(args, 0);
      string msg = Utils.GetSafeString(args, 1);
      string buttonOK = Utils.GetSafeString(args, 2, "Dismiss");
      string actionOK = Utils.GetSafeString(args, 3);
      string buttonCancel = Utils.GetSafeString(args, 4);
      string actionCancel = Utils.GetSafeString(args, 5);

      AlertDialog.Builder dialog = new AlertDialog.Builder(MainActivity.TheView);
      dialog.SetMessage(msg).
             SetTitle(title);
      dialog.SetPositiveButton(buttonOK,
          (sender, e) => {
            dialog.Dispose();
            if (!string.IsNullOrWhiteSpace(actionOK)) {
              UIVariable.GetAction(actionOK, "\"" + buttonOK + "\"", "1");
            }
          });
      if (!string.IsNullOrWhiteSpace(buttonCancel)) {
        dialog.SetNegativeButton(buttonCancel,
            (sender, e) => {
              dialog.Dispose();
              if (!string.IsNullOrWhiteSpace(actionCancel)) {
                UIVariable.GetAction(actionCancel, "\"" + buttonCancel + "\"", "0");
              }
            });
      }

      dialog.Show();
      return Variable.EmptyInstance;
    }
  }
  public class SpeakFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);

      string phrase = args[0].AsString();
      TTS.Voice = Utils.GetSafeString(args, 1, TTS.Voice).Replace("-", "_");
      TTS.SpeechRate = (float)Utils.GetSafeDouble(args, 2, TTS.SpeechRate);
      TTS.PitchMultiplier = (float)Utils.GetSafeDouble(args, 3, TTS.PitchMultiplier);
      TTS.Volume = (float)Utils.GetSafeDouble(args, 4, TTS.Volume);

      TTS tts = TTS.GetTTS(TTS.Voice);
      tts.Speak(phrase);

      return Variable.EmptyInstance;
    }
  }
  public class SpeechOptionsFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2, m_name);

      string option = args[0].AsString();
      Variable optionValue = Utils.GetSafeVariable(args, 1);
      Utils.CheckNotNull(optionValue, m_name);

      switch (option) {
        case "sound":
          TTS.Sound = optionValue.AsInt() == 1;
          break;
        case "voice":
          TTS.Voice = optionValue.AsString();
          break;
        case "speechRate":
          TTS.SpeechRate = (float)optionValue.AsDouble();
          break;
        case "volume":
          TTS.Volume = (float)optionValue.AsDouble();
          break;
        case "pitch":
          TTS.PitchMultiplier = (float)optionValue.AsDouble();
          break;
      }

      return Variable.EmptyInstance;
    }
  }
  public class VoiceFunction : ParserFunction
  {
    string m_action;
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);

      m_action = args[0].AsString();
      STT.Voice = Utils.GetSafeString(args, 1, STT.Voice).Replace("-", "_");
      string prompt = Utils.GetSafeString(args, 2);

      STT.VoiceRecognitionDone += OnVoiceResult;
      string init = STT.StartVoiceRecognition(prompt);
      if (init != null) {
        // An error in initializing:
        UIVariable.GetAction(m_action, "\"" + init + "\"", "");
      }

      return Variable.EmptyInstance;
    }
    protected void OnVoiceResult(string status, string recognized)
    {
      UIVariable.GetAction(m_action, "\"" + status + "\"",
                                     "\"" + recognized + "\"");
    }
  }
  public class StopVoiceFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      // No op at the moment.
      script.MoveForwardIf(Constants.END_ARG);
      return Variable.EmptyInstance;
    }
  }

  public class LocalizedFunction : ParserFunction
  {
    public static string Voice { get; set; } = "en-US";
    static Dictionary<string, Dictionary<string, string>> m_resources =
       new Dictionary<string, Dictionary<string, string>>();

    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);

      string key = args[0].AsString();
      Voice = Utils.GetSafeString(args, 1, Voice);

      Dictionary<string, string> resourceCache;
      if (!m_resources.TryGetValue(Voice, out resourceCache)) {
        resourceCache = new Dictionary<string, string>();
      }
      string localized;
      if (resourceCache.TryGetValue(Voice, out localized)) {
        return new Variable(localized);
      }

      localized = Localization.GetText(key);
      resourceCache[key] = localized;
      m_resources[Voice] = resourceCache;

      return new Variable(localized);
    }
  }
  public class InitAds : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2, m_name);

      string appId = args[0].AsString();
      string interstId = Utils.GetSafeString(args, 1);
      string bannerId = Utils.GetSafeString(args, 1);

      AdMob.Init(MainActivity.TheView, appId, interstId, bannerId);

      return Variable.EmptyInstance;
    }
  }
  public class ShowInterstitial : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      AdMob.ShowInterstitialAd();
      return Variable.EmptyInstance;
    }
  }
  public class InitIAPFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);

      List<string> keyParts = new List<string>();
      for (int i = 0; i < args.Count; i++) {
        string keyPart = Utils.GetSafeString(args, i);
        keyParts.Add(keyPart);
      }

      IAP.Init(keyParts);

      return Variable.EmptyInstance;
    }
  }
  public class RestoreFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);
      string strAction = args[0].AsString();

      for (int i = 1; i < args.Count; i++) {
        string productId = Utils.GetSafeString(args, i);
        IAP.AddProductId(productId);
      }

      UnsubscribeFromAll();

      IAP.IAPOK += (productIds) => {
        UIVariable.GetAction(strAction, "", "\"" + productIds + "\"");
      };
      IAP.IAPError += (errorStr) => {
        UIVariable.GetAction(strAction, "\"" + errorStr + "\"", "");
        RestoreFunction.UnsubscribeFromAll();
      };

      IAP.Restore();

      return Variable.EmptyInstance;
    }
    public static void UnsubscribeFromAll()
    {
      Delegate[] clientList = IAP.IAPOK?.GetInvocationList();
      if (clientList != null) {
        foreach (var d in clientList) {
          IAP.IAPOK -= (OnIAP)d;
        }
      }
      clientList = IAP.IAPError?.GetInvocationList();
      if (clientList != null) {
        foreach (var d in clientList) {
          IAP.IAPError -= (OnIAP)d;
        }
      }
    }
  }
  public class PurchaseFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2, m_name);
      string strAction = args[0].AsString();
      string productId = args[1].AsString();

      RestoreFunction.UnsubscribeFromAll();

      IAP.IAPOK += (productIds) => {
        UIVariable.GetAction(strAction, "", "\"" + productIds + "\"");
        RestoreFunction.UnsubscribeFromAll();
      };
      IAP.IAPError += (errorStr) => {
        UIVariable.GetAction(strAction, "\"" + errorStr + "\"", "");
        RestoreFunction.UnsubscribeFromAll();
      };

      IAP.Purchase(productId);

      return Variable.EmptyInstance;
    }
  }
  public class ReadFileFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);
      string strFilename = args[0].AsString();

      List<Variable> results = new List<Variable>();
      AssetManager assets = MainActivity.TheView.Assets;
      using (StreamReader sr = new StreamReader(assets.Open(strFilename))) {
        while (!sr.EndOfStream) {
          Variable varLine = new Variable(sr.ReadLine());
          results.Add(varLine);
        }
      }
      return new Variable(results);
    }
  }
  public class PauseFunction : ParserFunction
  {
    static Dictionary<string, System.Timers.Timer> m_timers =
       new Dictionary<string, System.Timers.Timer>();

    bool m_startTimer;

    public PauseFunction(bool startTimer)
    {
      m_startTimer = startTimer;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);

      if (!m_startTimer) {
        Utils.CheckArgs(args.Count, 1, m_name);
        string cancelTimerId = Utils.GetSafeString(args, 0);
        System.Timers.Timer cancelTimer;
        if (m_timers.TryGetValue(cancelTimerId, out cancelTimer)) {
          cancelTimer.Stop();
          cancelTimer.Dispose();
          m_timers.Remove(cancelTimerId);
        }
        return Variable.EmptyInstance;
      }

      Utils.CheckArgs(args.Count, 2, m_name);
      int timeout = args[0].AsInt();
      string strAction = args[1].AsString();
      string owner = Utils.GetSafeString(args, 2);
      string timerId = Utils.GetSafeString(args, 3);
      bool autoReset = Utils.GetSafeInt(args, 4, 0) != 0;

      System.Timers.Timer pauseTimer = new System.Timers.Timer(timeout);
      pauseTimer.Elapsed += (sender, e) => {
        if (!autoReset) {
          pauseTimer.Stop();
          pauseTimer.Dispose();
          m_timers.Remove(timerId);
        }
        //Console.WriteLine("QuizTimer_Elapsed {0:HH:mm:ss.fff}", e.SignalTime);
        MainActivity.TheView.RunOnUiThread(() => {
          UIVariable.GetAction(strAction, owner, "\"" + timerId + "\"");
        });

      };
      pauseTimer.AutoReset = autoReset;
      m_timers[timerId] = pauseTimer;

      pauseTimer.Start();

      return Variable.EmptyInstance;
    }
  }
  public class GetDeviceLocale : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      script.MoveForwardIf(Constants.END_ARG);
      return new Variable(Localization.GetDeviceLangCode());
    }
  }
  public class SetAppLocale : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      string code = Utils.GetItem(script).AsString();
      Utils.CheckNotEmpty(script, code, m_name);

      bool found = Localization.SetProgramLanguageCode(code);
      return new Variable(found);
    }
  }
  public class TranslateTabBar : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      MainActivity.TranslateTabs();
      script.MoveForwardIf(Constants.END_ARG);
      return Variable.EmptyInstance;
    }
  }
  public class GetSettingFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 1, m_name);
      string settingName = args[0].AsString();
      string strType = Utils.GetSafeString(args, 1, "string");
      Variable defValue = Utils.GetSafeVariable(args, 2);

      switch(strType) {
        case "float":
        case "double":
          float def = defValue == null ? -1 : (float)defValue.AsDouble();
          float result = Settings.GetFloatSetting(settingName, def);
          return new Variable(result);
        case "int":
          int defInt = defValue == null ? -1 : defValue.AsInt();
          int resultInt = Settings.GetIntSetting(settingName, defInt);
          return new Variable(resultInt);
        case "bool":
          bool defBool = defValue == null ? false : defValue.AsInt() != 0;
          bool resultBool = Settings.GetBoolSetting(settingName, defBool);
          return new Variable(resultBool);
        default:
          string defStr = defValue == null ? null : defValue.AsString();
          string resultStr = Settings.GetSetting(settingName, defStr);
          return new Variable(resultStr);
      }
    }
  }
  public class SetSettingFunction : ParserFunction
  {
    protected override Variable Evaluate(ParsingScript script)
    {
      bool isList = false;
      List<Variable> args = Utils.GetArgs(script,
                            Constants.START_ARG, Constants.END_ARG, out isList);
      Utils.CheckArgs(args.Count, 2, m_name);
      string settingName = args[0].AsString();
      Variable settingValue = Utils.GetSafeVariable(args, 1);
      string strType = Utils.GetSafeString(args, 2, "string");

      switch (strType) {
        case "float":
        case "double":
          float setting = (float)settingValue.AsDouble();
          Settings.SaveSetting(settingName, setting);
          break;
        case "int":
          int settingInt = settingValue.AsInt();
          Settings.SaveSetting(settingName, settingInt);
          break;
        case "bool":
          bool settingBool = settingValue.AsInt() != 0;
          Settings.SaveSetting(settingName, settingBool);
          break;
        default:
          string settingStr = settingValue.AsString();
          Settings.SaveSetting(settingName, settingStr);
          break;
      }

      return Variable.EmptyInstance;
    }
  }
}
