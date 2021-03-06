﻿using Healthcare020.Mobile.Helpers;
using Healthcare020.Mobile.Resources;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Healthcare020.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PreglediMainPage
    {
        private NavigationPage NavigationPageParent;

        public PreglediMainPage()
        {
            Title = AppResources.PreglediTabTitle;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            NavigationPageParent = (NavigationPage)Parent;
        }

        private async void ZakazaniPregledi_OnTapped(object sender, EventArgs e)
        {
            await ZakazaniPreglediButton.ScaleEffect(1.1);
            await NavigationPageParent.PushAsync(new PreglediPage(OnlyZakazani: true), true);
        }

        private async void SviPregledi_OnTapped(object sender, EventArgs e)
        {
            await SviPreglediButton.ScaleEffect(1.1);
            await NavigationPageParent.PushAsync(new PreglediPage(), true);
        }

        private async void NoviPregled_OnTapped(object sender, EventArgs e)
        {
            await ZakazivanjePregledaButton.ScaleEffect(1.1);
            await NavigationPageParent.PushAsync(new NoviZahtevZaPregledPage(), true);
        }
    }
}