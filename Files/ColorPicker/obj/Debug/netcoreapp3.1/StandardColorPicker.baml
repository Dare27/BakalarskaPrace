   M S B A M L   `   `   `  ����_  [Microsoft.Xaml.Behaviors, Version=1.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3aeHclr-namespace:Microsoft.Xaml.Behaviors;assembly=Microsoft.Xaml.BehaviorsMicrosoft.Xaml.Behaviors  5      R NColorPicker, Version=3.3.1.0, Culture=neutral, PublicKeyToken=1c61eec504ce2276?$clr-namespace:ColorPicker.ConvertersColorPicker.Converters 5      =#clr-namespace:ColorPicker.BehaviorsColorPicker.Behaviors 5      C&clr-namespace:ColorPicker.UserControlsColorPicker.UserControls 5	      C&clr-namespace:ColorPicker.UIExtensionsColorPicker.UIExtensions 5
      )clr-namespace:ColorPickerColorPicker 5      %   ColorPicker.StandardColorPicker   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ViHclr-namespace:Microsoft.Xaml.Behaviors;assembly=Microsoft.Xaml.Behaviors     5      ;
converters$clr-namespace:ColorPicker.Converters     5      :
behaviours#clr-namespace:ColorPicker.Behaviors     5      ;controls&clr-namespace:ColorPicker.UserControls     5	      5ui&clr-namespace:ColorPicker.UIExtensions     5
      +localclr-namespace:ColorPicker     5      -   5        '  !ColorPicker.DualPickerControlBase   Name$  uc��5      $	��200��5      $	��380��5       � 	Resources 5      5 /ColorPicker.Converters.PickerTypeToIntConverter   PickerTypeToIntConverter�� 5   
   %   &	          5      62   5      .��5      � -   5        $��MainGrid��5       � RowDefinitions 5   
   �� 5      $��6* �6   �� 5      $
��1.4* �6   �� 5      $
��0.5* �6   �� 5      $
��1.1* �6   $��35��6*   �� 5      $��4* �6   $	��110��6(    � ColumnDefinitions 5   
   �� 5      $	��89* �6   $	��100��6+   �� 5       $
��142* �6   �� 5!      $	��37* �6   .+�5+   
     ColorPicker.SquarePicker  $��3��6   $��2��61   $��5 5 5 15q�6B      
PickerType 5,      ��  �� ElementName$ uc�� �� Path$ 
PickerType�   SmallChange 5-      �� $ uc��$ SmallChange�#  ColorPicker.PickerControlBase   
ColorState 5.      �� $ uc��$ 
ColorState�	 �� Mode$	 TwoWay=�  ColorPicker.ColorDisplay  5/   
   $��3��6   $��1��6.   $��5,5,5,5q�6;   $��80��6L    50      �� $ uc��$ 
ColorState�$	 TwoWay=�
   SecondColorState
 51      �� $ uc��$ SecondColorState�$	 TwoWay=�   HintColorState 52      �� $ uc��$ HintColorState�$	 TwoWay=�   UseHintColor 53      �� $ uc��$ UseHintColor�$	 TwoWay=�]� 54   
   $��1��6   $��1��6!   $��3��61   $	��Top=�6B   $��Right=�6Z   $��	0,40,10,0q�6v   $��2��6�   .+�55      �� $	��Hex��6   !  ColorPicker.HexColorTextBox  56      $��30��57   Y    56   $   �� $ uc��$ 
ColorState�$	 TwoWay=�   	ShowAlpha 57   $   �� $ uc��$ 	ShowAlpha�6Y   58        ColorPicker.ColorSliders  59   
   $��2��6   $��3��6*   $��3��6>   $��	0,20,0,-5q�5<   R    5:      �� $ uc��$ 
ColorState�$	 TwoWay=�   SmallChange 5;      �� $ uc��$ SmallChange�   	ShowAlpha 5<      �� $ uc��$ 	ShowAlpha�6R   5=      5>      