﻿using System;
using Healthcare020.Mobile.Helpers;
using Healthcare020.Mobile.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Healthcare020.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterPage : BaseValidationContentPage
    {
        public RegisterViewModel RegisterVM;

        public RegisterPage()
        {
            InitializeComponent();
            BindingContext = RegisterVM = ViewModelLocator.RegisterViewModel;

            //Validation requirements
            BaseValidationVM = RegisterVM;
            SetFormBodyElement();
            SetErrorsClearOnTextChanged();
        }

        private void RegisterButton_OnClicked(object sender, EventArgs e)
        {
            ValidateModel();

        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage=new WelcomePage();
        }
    }
}