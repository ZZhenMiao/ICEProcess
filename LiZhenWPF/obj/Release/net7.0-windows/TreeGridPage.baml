   M S B A M L   `   `   `  ���� D  @LiZhenWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:LiZhenWPF	LiZhenWPF      LiZhenWPF.TreeGridPage   W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     )localclr-namespace:LiZhenWPF       N� Title$  TreeGridPage�� � 	Resources &    LiZhenWPF.LevelToMarginConverter   LevelToIndentConverter  ExpandCollapseToggleStyle �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius J� SnapsToDevicePixels �� 
StartPoint	 �� EndPoint
 � Color � Offset F�Name �� Triggers P� Property P� Value �� 
TargetName  CellTemplate_Name d�Name �� 	ClickMode �� 	Converter d� 	IsChecked �� Path �� RelativeSource �� AncestorType �� Triggers �� Value �� Binding6  0System.Windows.Controls.GridViewColumnCollection  gvcc �� CellTemplate �� Width �� DisplayMemberBinding �� Padding ��Name  �� Content! �� Columns" ��Name# ]� 
Conditions$ �� Property% �� Value��%\  &	        &	      &	 L    7  7   &	 ?    7  'Q� �    7  'R� �    7    �� " d�.���  � |�$ False����  � ��$ 19����  � ��$ 13����  � �� �� " d�.R��� $��19��$��13��
������ ._��� $��9��$��9��$��1q����	#FF7898B5$ 1��$
 true������ $	 0,0&�$		 1,1&���� $
 White��$ .2Q�� $
 	#FFC0B7A6��$ 1Q�._�F� $ 
ExpandPath��$��1,1,1,1q�
���   �s����    �� �    ��- р� ��- р� @KL ���- @KL ���- ��- �@KL ��- �@KL �� ���- �� ���-     р�     р� �� 	 P�  �@d�	IsChecked$
 TrueY�. ��� $ 
ExpandPath�� � ��+ ��    �� �    ��- �@KL ��- �@KL �� 	�� .R�]� .+�d� $ Expander��8��  $ Press=����� *Level+8   �� $ 
IsExpanded� �� " Q��� ���� *Name+ �� $ False�� �� $ HasItems� �� " Q�.a��� $ Expander�� � y�$ Hidden=�  �� $
��Name��8   �� $��JobTitle��$ 60�� �� *
JobTitle+�� $	��Age��$ 60�� �� *Age+�� $	��Sex��$ 60�� �� *Sex+�� " Q�.���  � �� �� " Q�.R��� .+��� $��Bd��#��z��#��z��#��z��# z��._��� $ PART_Header��#  z��8!   �� $" 	ItemsHost�� P�  �@Q�
IsExpanded$ false��. ��� $ 	ItemsHost�� � y�$ 	Collapsed=�]� # �� $ � ��$% false���� $ � ��$
% Auto��./��� $ PART_Header�� � ��$ 75��]� # �� $ � ��$% false���� $ � ��$
% Auto��./��� $ PART_Header�� � ��$ 19��P�  �@Q�
IsSelected$
 true��. ��� $ Bd��# � �� � ���� # � �� � ��]� # �� $ �@Q�
IsSelected$
% true���� $ �@Q�IsSelectionActive$% false��./��� $ Bd��# � �� � ���� # � �� � ��P�  � {�$ false��. ��� # � �� � ���� " R�.���  � �� �� " R�.R��� #��z��#��z��._�]� .+��� 8!   $	��Top=��� .��� .+�R� -   $��_list��$��0q�$��0q�$��Stretch=�
������ ���� *
Children+���� & �� ItemsSource& �� *
Children+.R��� ' ��Name$
' back��$ 0��$��1q�$��70��
������ ( � DataContext( �� ._��� ) �� Orientation$) 
Horizontal=�$��2q�.+��� $	��2 0q����� *Text+