   M S B A M L   `   `   `  ����I  EEyeTribeCSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:EyeTribeCSharpEyeTribeCSharp  5          EyeTribeCSharp.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5	      @d2http://schemas.microsoft.com/expression/blend/2008     5
      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      7EyeTribeCSharpclr-namespace:EyeTribeCSharp     5        � AllowsTransparency  .  � Title$ Eye Tribe C# Project��5      $
��1200��5      $	��800��5       � WindowStartupLocation$ CenterScreen=�5       � WindowStyle$
 None=�5       � WindowState$ 	Maximized=�5       � 	Resources 5          EyeTribeCSharp.ImageButton �� 
TargetType �� Property �� Value	 �� 
TargetType
 ��Name �� RelativeSource �� Path �� Triggers P� Property P� Value �� 
TargetName3  -System.Windows.Media.Effects.DropShadowEffect   
BlurRadius   Color   ShadowDepth   lineargradbrush �� 
StartPoint �� EndPoint � Color � Offset  semitransbrush �� Color��%�  '        &	  �    &	 i    �� 5   
   "  6   .�5      ��  � �� 5      �� 5      "	  6&   .R�5      � $��5,5,5,5q�6   .+�5      �� $
 VisibleIcon��6$   $
��Fill=�5   !   $	��100��5   !   $	��100��5   !   ��5   !   ��  �� *TemplatedParent+$
 Icon�5   !   5        5!      P� 5"       �@d�	IsChecked$
 TrueY�6;   . �5#   "   �� $ VisibleIcon��6)    �@J�Effect 5$   &     5%   *   $	 100Q�6;   $ White��6L   $ 0Q�6Z   5'   #   5(      P� 5)       �@d�	IsChecked$ FalseY�6;   . �5*   "   �� $ VisibleIcon��6)    �@J�Effect 5+   &     5,   *   $ 0Q�6;   $ White��6J   $ 0Q�6X   5.   #   5/      51      53      54      �� 55   
   $	 0,0&�66   $	 1,1&�6G   ��57      � $ Transparent��6   $ 0Q�63   � 58      $ Yellow��6   $ 1Q�6.   5:      �� 5;   
   $ 	#80f2f264��61   6E   5<      .��5>      � -   5         �Name$ 
LayoutRoot��5>      $y�Visible=�6   .+�5?   
   �� -   5         ��Name$ WebImageScroll��5?      $��Hidden=�6/   $��Hidden=�6T   $��Center=�6{   .��5@      �� -   5        $
 WebImage��5@      5A      ]� 5B   
   $
��Left=�6   
������ ��.  .+�5C      �� -   5        $��Receive_Status_Text��5C   0   $��Bottom=�6   $��266.67��6K   $��Receiving Data��6]   $��18(�6s   $��Center=�6�   $y�Hidden=�6�   5D      ]� 5E   
   $��Right=�6   
������ ��.  .+�5F      �� -   5        $��Share_Status_Text��5F   0   $��Bottom=�6   $��266.67��6I   $��Sharing Data��6[   $��18(�6o   $��Center=�6}   $y�Hidden=�6�   5G      �� 5H   
   -   5         ��Name$ canvas_overlay��5H      .+�5I      A� -   5         A�Name$ GazePointer��5I      $��32��6+   $��32��66   $y�Hidden=�6B   ��5J      �� 6     �� ImageSource$ Graphics\eye-7-32.png��6+   5K      A� 5L      -   5        $ ReceiveGazePointer��5L      $��32��62   $��32��6=   $y�Hidden=�6I   ��5M      �� 6    $! Graphics\green-eye-7-32.png��6+   5N      A� 5O      -	   5        $ AnswerEllipse��5O      $	��100��6-   $��60��69   
����  �$y�Hidden=�6S   5P      �� 5Q   
   -
   5        $ track_overlay��5Q       J� Opacity$ 1Q�6(   $��10,-10,-10,10q�64   .+�5R      A� -   5        $ GazeSpot��5R      $��8��6(   $��8��62   
���  ��$y�Hidden=�6H   $	 0.4Q�6\   A� 5S      -   5        $ PrevGazeSpot��5S      $��8��6,   $��8��66   
���  ��$y�Hidden=�6L   $	 0.4Q�6`   �� 5T      -   5         ��Name$ SpotLine��5T      
���  ��	  �� X1$  0��62   	! �� Y1$! 25��69   	" �� X2$" 0��6A   	# �� Y2$# 25��6H   $y�Hidden=�6P   5U      �� 5V   
   -   5        $ ��Name$$ TestBox��5V      $	 0.0Q�6#   $��24(�61   $��10,250,-10,341q�6?   % �� TextWrapping$
% Wrap=�6W   $[��UStarting. Please make sure Eye Tracker is calibrated and your eyes are on the screen!��6k   �� 5W   
   -   5        & ��Name$& 	mainimage��5W      ' �� Orientation$' 
Horizontal=�6(   $��Center=�6A   $��Center=�6\   $y�Visible=�6y   .+�5X      �� $' Vertical=�6   $y�Visible=�60   .+�5Y      �� $' 
Horizontal=�6   $y�Visible=�67   .+�5Z      �� -   5        $
 lefthemifield��5Z      $,��&Graphics/newgraphics/lefthemifield.png��63   $	��252��6c   $��Uniform=�6o   $y�Visible=�6�   ( J� Effect( 5[        5\      )   	Direction$	) 225Q�6/   $ 	#FFB6C2CB��6?   $ 10Q�6Q   $ 10Q�6b   5^      �� 5_      -   5        $
 righthemifield��5_      $-��'Graphics/newgraphics/righthemifield.png��64   $	��252��6e   $��Uniform=�6q   $y�Visible=�6�   5`      5a      �� 5b      $' 
Horizontal=�6   .+�5c      �� -   5        $
 leftoptictract��5c      $-��'Graphics/newgraphics/leftoptictract.png��64   $	��252��6e   $��Uniform=�6q   5d      �� 5e      -   5        $
 rightoptictract��5e      $.��(Graphics/newgraphics/rightoptictract.png��65   $	��252��6g   $��Uniform=�6s   5f      5g      �� 5h      $' 
Horizontal=�6   .+�5i      �� -   5        * ��Name$* filler��5i       $��244.8��60   
������ 6S   �� 5j      -   5        $
 middlerightfiller��5j      $*��$Graphics/newgraphics/middleright.png��67   $��255.6��6e   $��Uniform=�6s   5l      5m      �� 5n      $' 
Horizontal=�6   .+�5o      �� -   5        $
 leftvisualcortex��5o      $/��)Graphics/newgraphics/leftvisualcortex.png��66   $	��252��6i   $��Uniform=�6u   5p      �� 5q      -   5        $
 rightvisualcortex��5q      $0��*Graphics/newgraphics/rightvisualcortex.png��67   $	��252��6k   $��Uniform=�6w   5r      5s      5t      �� 5u      $' Vertical=�6   .+�5v      �� $��212.4��6   
������ 6@   �� 5w      -   5        $
 opticradiations��5w      $.��(Graphics/newgraphics/opticradiations.png��61   $	��207��6c   $��Uniform=�6o   $y�Visible=�6�   5x      5y      5z      � 5|   
   -   5        $ GridTop��5|      $��0q�6    $	��Top=�6+   $	��250��6C   + � RowDefinitions+ 5}      �� 5~      $��5* �6    �� 5      $��1* �6    .+�5�      �� ���	#CC0000006   �� 5�      ���	#CC808080$��0,0,0,4q�6-   �� 5�      ���	#CCA9A9A9$��0,0,0,2q�6-   � 5�      -   5        $ GridButtons��5�   0   $��0��6   $
��1600��6    , � ColumnDefinitions, 5�      �� 5�      �� 5�      �� 5�      �� 5�      �� 5�      �� 5�      .+�5�        -   5        -  Name$- Receive��5�   Y   $��0��6I   .   Icon$". Graphics/eye-white-7-120.png��6j   / � FocusVisualStyle/ 6-   Z� 6j     5�      -   5        $- Share��5�   Y   $��1��6I   $#. Graphics/share-white-4-96.png��6h   / 6-   Z� 6h     5�      -   5        $- Trace��5�   Y   $��2��6I   $". Graphics/dots-white-7-96.png��6h   / 6-   Z� 6h     5�      -   5        $- 	Highlight��5�   Y   $��3��6I   $". Graphics/flag-white-4-96.png��6l   / 6-   Z� 6l     5�      -   5        $
- Exit��5�   Y   $��4��6I   $. Graphics/exit.png��6g   / 6-   Z� 6g     5�      -    5        $- startrecording��5�   Y   $��5��6I   $". Graphics/microphone-3-96.png��6q   / 6-   Z� 6q   5�      � 5�      -!   5        $ ButtonsStatus��5�   0   $��1��6   $
��1600��6    $��	0,0,0,-46q�6G   , 5�      �� 5�      �� 5�      �� 5�      �� 5�      �� 5�      �� 5�      .+�5�      � -"   5        $ ReceiverStatus��5�   7   $��0��6   $
��1600��6'   .+�5�      �� -#   5        $��Receive_Text��5�       $��266.67��64   $��Receive off��6F   $��18(�6Y   $��Center=�6g   6�   5�      � 5�      -$   5        $ SenderStatus��5�   7   $��1��6   $
��1600��6'   .+�5�      �� -%   5        $��
Share_Text��5�       $��266.67��62   $��	Share off��6D   $��18(�6U   $��Center=�6c   6�   � 5�      -&   5        0 �Name$0 
Sender_Pop��5�      ��.  $��0,0,0,0q�6?   .��5�      �� -'   5        $$ Sender_Pop_TextBox��5�   #   $"��Please enter your IP address��6?   $��Stretch=�6c   6�   5�      5�      �� 5�      -(   5        $��
Track_Text��5�      $��2��6.   $��266.67��6>   $��	Track off��6P   $��18(�6a   $��Center=�6o   6�   �� 5�      -)   5        $��Highlight_Text��5�      $��3��62   $��266.67��6B   $��Highlight off��6T   $��18(�6i   $��Center=�6w   6�   �� 5�      -*   5        $��	Exit_Text��5�      $��4��6-   $��266.67��6=   6R   � 5�      -+   5        $ DataRecordStatus��5�   7   $��5��6   $
��1600��6'   .+�5�      �� -,   5        $��Data_Record_Status_Text��5�       $��266.67��6?   $��Data Record off��6Q   $��18(�6h   $��Center=�6v   6�   � 5�      --   5        $0 Data_Record_Pop��5�      ��.  $��0,0,0,0q�6D   .��5�      �� -.   5        $$ Data_Record_Pop_TextBox��5�   #   $(��"Please enter number on this laptop��6D   $��Stretch=�6n   6�   5�      5�      5�      5�      5�      5�      