   M S B A M L   `   `   `  ����R  NColorPicker, Version=3.3.1.0, Culture=neutral, PublicKeyToken=1c61eec504ce2276C&clr-namespace:ColorPicker.UIExtensionsColorPicker.UIExtensions  5      C&clr-namespace:ColorPicker.UIExtensionsColorPicker.UIExtensions  5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089   RepeatButtonTransparent  �� 
TargetType �� Property �� Value �� 
TargetType   SliderThumbHorizontalDefault � UseLayoutRounding  PreviewColorSliderTemplate1   +ColorPicker.UIExtensions.PreviewColorSlider ��Name �� CornerRadius J� SnapsToDevicePixels � RowDefinitions	 �� RelativeSource
 �� Mode �� Path m�Name m� Fill m� 	Placement ��Name ]�Name ]� DecreaseRepeatButton �� DecreaseLarge ]� IncreaseRepeatButton �� IncreaseLarge ]� Thumb n�Name � OverridesDefaultStyle �� Triggers P� Property P� Value �� 
TargetName-  'ColorPicker.UIExtensions.HsvColorSlider-  'ColorPicker.UIExtensions.RgbColorSlider-  'ColorPicker.UIExtensions.HslColorSlider�� 5      F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     :x,http://schemas.microsoft.com/winfx/2006/xaml     5      -ui&clr-namespace:ColorPicker.UIExtensions  5      0local&clr-namespace:ColorPicker.UIExtensions  5      %4  &	        &	 Z    &	 �    7   7   7  '  
    7  '  X
    7  '  �
    7  �� 5      "  ��6,   .�5   
   ��  �@�OverridesDefaultStyle$
 true��62   �� 5   
    � ��
 ���� 6'   �� 5   
    � |�$ false��6&   �� 5	   
    � ��$ false��6&   �� 5
   
    � �� 5      �� 5      " ��6"   .R�5      �� #��z��6    #��z��6D   #��z��5       5      5      5      �� 5      " n�6;   .R�5   
   � $��Center=�6   $
 True��6,   $��Center=�6E   .+�5      A� $��12��6   $��12��6!   $}�False��6-   
������ 
���   �$��2��6d   $��Center=�6x   $��Center=�6�   A� 5      $��10��6   $��10��6!   $}�False��6-   
������ ���#ffc107$	��1.5��6f   $��Center=�6|   $��Center=�6�   5      5      �� 5      "   69   .R�5   
   �� $ border��6   $ 5��6!   #��z��5      #��z��6E   $
 True��5      ._�5      �  5       �� 5!      $
��Auto �6$   �� 5"      $
��Auto �6$   #��z��62   �� 5#      $
��Auto �6$   .+�5%      �� $
��Left=�6   $ 5,0,0,5��64   $��3��6K   $��6��6\   $��12��6f   ��5&      �� 	 �� *TemplatedParent+$
 OneWay=�$ LeftCapColor��� 5'      $��Right=�6   $ 0,5,5,0��65   $��3��6L   $��6��6]   $��12��6g   ��5(      �� 	 �� *TemplatedParent+$
 OneWay=�$ RightCapColor��� 5)      $��3��6   #��z��6*   $	��6,0q�6T   $��Stretch=�5*      $��Stretch=�67   5+      m� 5,      $ TopTick��6   # z��6+   $��4��6O   $��0,0,0,2q�6Z   $	 Top=�5-      $��0��6*   $y�	Collapsed=�67   m� 5.      $ 
BottomTick��6   # z��6.   $��4��6R   $��0,2,0,0q�6]   $ Bottom=�5/      $��2��6-   $y�	Collapsed=�6:   �� 50      $ TrackBackground��6   
������ $��0q�6K   $��0��6_   $	��5,0q�6j   $��1��51      $��center=�6&   ._�52      �� $��-6,-1q�6   .+�53      �� $ PART_SelectionRange��6$   #��� ��54   $   $	��4.0��6g   $y�Hidden=�55   $   56      57      ]� 58      $ 
PART_Track��6   $��1��6,    59      �� 5:      #��Z 6'   8��  5;   '    5=      �� 5>      #��Z 6'   8�� 5?   '    5A      n� 5B      $ Thumb��6    $|�False��6/   $��12��6A   $
 True��6M   8�� $��Center=�5C   Y   $��12��5D       5F      5G      5H       5I   
   P� 5J       �@��IsSelectionRangeEnabled$
 true��69   . �5K      �� $ PART_SelectionRange��6/    � y�$ Visible=�6P   5L      P� 5M       �@J�IsKeyboardFocused$
 true��63   . �5N      �� $ Thumb��6/    � ��
 ��  �6B   5O      5Q      �� 5S      "   6   .�5T   
   ��  � ��8   6%   5U      �� 5V      "   6   .�5W   
   ��  � ��8   6%   5X      �� 5Y      "   6   .�5Z   
   ��  � ��8   6%   5[      5\      