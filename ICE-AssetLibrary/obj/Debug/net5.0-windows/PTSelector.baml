   M S B A M L   `   `   `  ����D  @ICE-Model, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null8*clr-namespace:ICE_Model;assembly=ICE-Model	ICE_Model  5   	   K GICE-AssetLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:ICE_AssetLibraryICE_AssetLibrary 5   	   !   ICE_AssetLibrary.PTSelector   5   	   W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   <model*clr-namespace:ICE_Model;assembly=ICE-Model     5   	   0localclr-namespace:ICE_AssetLibrary     5   	   $	��400��5	   	   $	��210��6     � 
ResizeMode$  	CanResize=�6"    � WindowStartupLocation$ CenterScreen=�69    � WindowStyle$ 
ToolWindow=�6^    � 	Resources 5
         TreeBoxItem �� 
TargetType �� Property �� Value �� ItemsSource �� Path	 d� 	IsChecked��%;  &	        �� 5   
   " R�6$   .�5      ��  � �� 5      �� 5       6/   �� $ Children�.R�5      �� ��6#   �� 	 67   �� *
IsSelected+{�6X   �� *	IsEnabled+��5      �� 5   "   .R�5   &   �� ��60   �� *Name+5   #   5      5      5      5      5      .��5      � 
 � RowDefinitions
 5   
   �� 5      $��1* �6   �� 5      $��28 �6   .+�5    
   R� -   5         R�Name$ Container_TreeView��5       #��[  6/   ��5!      �� 5"      " Q�6   .�5#      ��  �@Q�
IsExpanded$
 True��63   5$      5&      � 5'   
   $��1��6    � ColumnDefinitions 5(      �� 5)      $��* �6#   �� 5*      $��* �6#   .+�5,      �� -   5         ��Name$ Cancel_Button��5,      $��0��6,   $��取消��6<   6I   �� 5-      -   5        $ Apply_Button��5-      $��1��6+   $��应用��6;   6H   5.      5/      50      