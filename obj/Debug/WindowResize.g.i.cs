﻿#pragma checksum "..\..\WindowResize.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "64BA596EC621CA9E109DCCB4D2054B8F27A44059"
//------------------------------------------------------------------------------
// <auto-generated>
//     Tento kód byl generován nástrojem.
//     Verze modulu runtime:4.0.30319.42000
//
//     Změny tohoto souboru mohou způsobit nesprávné chování a budou ztraceny,
//     dojde-li k novému generování kódu.
// </auto-generated>
//------------------------------------------------------------------------------

using BakalarskaPrace;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace BakalarskaPrace {
    
    
    /// <summary>
    /// WindowResize
    /// </summary>
    public partial class WindowResize : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 14 "..\..\WindowResize.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox widthTextBox;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\WindowResize.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox heightTextBox;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\WindowResize.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox maintainAspectRatioCheckBox;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\WindowResize.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox resizeContentCheckBox;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/BakalarskaPrace;component/windowresize.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\WindowResize.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.widthTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 14 "..\..\WindowResize.xaml"
            this.widthTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.Width_TextChanged);
            
            #line default
            #line hidden
            
            #line 14 "..\..\WindowResize.xaml"
            this.widthTextBox.PreviewTextInput += new System.Windows.Input.TextCompositionEventHandler(this.NumberValidationTextBox);
            
            #line default
            #line hidden
            return;
            case 2:
            this.heightTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 18 "..\..\WindowResize.xaml"
            this.heightTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.Height_TextChanged);
            
            #line default
            #line hidden
            
            #line 18 "..\..\WindowResize.xaml"
            this.heightTextBox.PreviewTextInput += new System.Windows.Input.TextCompositionEventHandler(this.NumberValidationTextBox);
            
            #line default
            #line hidden
            return;
            case 3:
            this.maintainAspectRatioCheckBox = ((System.Windows.Controls.CheckBox)(target));
            
            #line 20 "..\..\WindowResize.xaml"
            this.maintainAspectRatioCheckBox.Checked += new System.Windows.RoutedEventHandler(this.MaintainAspectRatio_Checked);
            
            #line default
            #line hidden
            
            #line 20 "..\..\WindowResize.xaml"
            this.maintainAspectRatioCheckBox.Unchecked += new System.Windows.RoutedEventHandler(this.MaintainAspectRatio_Unchecked);
            
            #line default
            #line hidden
            return;
            case 4:
            this.resizeContentCheckBox = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 5:
            
            #line 23 "..\..\WindowResize.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Resize_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 24 "..\..\WindowResize.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Cancel_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

