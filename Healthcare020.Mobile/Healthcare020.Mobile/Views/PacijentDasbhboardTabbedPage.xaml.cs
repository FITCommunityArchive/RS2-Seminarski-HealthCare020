﻿using Healthcare020.Mobile.Constants;
using Healthcare020.Mobile.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;
using Application = Xamarin.Forms.Application;
using TabbedPage = Xamarin.Forms.TabbedPage;

namespace Healthcare020.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PacijentDasbhboardTabbedPage : TabbedPage
    {
        public BaseViewModel BaseVM { get; set; }

        public PacijentDasbhboardTabbedPage()
        {
            InitializeComponent();
            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
            //this.Children.Add(new WelcomePage());
            //this.Children.Add(new AboutPage());
            //this.Children.Add(new LoginPage());
            //this.Children.Add(new SettingsPage());
            this.SelectedTabColor = (Color)Application.Current.Resources[ResourceKeys.HealthcareCyanColor];
            this.UnselectedTabColor = (Color)Application.Current.Resources[ResourceKeys.CustomBlueColor];
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}