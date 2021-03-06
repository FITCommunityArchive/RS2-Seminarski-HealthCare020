﻿using Healthcare020.WinUI.Helpers.Dialogs;
using Healthcare020.WinUI.Models;
using Healthcare020.WinUI.Services;
using HealthCare020.Core.Constants;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Healthcare020.WinUI.Forms.AdminDashboard.PredefinedData
{
    public sealed partial class frmNewRole : Form
    {
        private static frmNewRole _instance;
        private readonly APIService _apiService;
        private TwoFieldsDto Role;

        public new static frmNewRole ShowDialog()
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new frmNewRole();
            }
            ((Form)_instance).ShowDialog();
            return _instance;
        }

        public static frmNewRole ShowDialogWithData(TwoFieldsDto role = null)
        {
            if (_instance == null || _instance.IsDisposed)
                _instance = new frmNewRole(role);
            else
                _instance.Role = role;

            ((Form)_instance).ShowDialog();
            return _instance;
        }

        private frmNewRole(TwoFieldsDto role = null)
        {
            InitializeComponent();
            Role = role;

            if (Role != null)
            {
                txtNaziv.Text = Role.Naziv;
            }

            _apiService = new APIService(Routes.RolesRoute);
            Text = Role == null ? Properties.Resources.frmNewRoleAdd : Properties.Resources.frmNewRoleUpdate;

            var mainFormSize = MainForm.Instance.Size;
            Size = new Size(mainFormSize.Width - 16, mainFormSize.Height - 14);
            pnlMain.MinimumSize = Size;
            FormBorderStyle = FormBorderStyle.None;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TransparencyKey = Color.Transparent;
            pnlMain.BackColor = Color.FromArgb(125, 0, 0, 0);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //IGNORE
        }

        private void frmNewRole_Shown(object sender, System.EventArgs e)
        {
            pnlDialog.PointToScreen(new Point(Width / 2, Height / 2));
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            {
                Errors.SetError(txtNaziv, Properties.Resources.RequiredField);
                return false;
            }

            if (txtNaziv.Text.Any(char.IsDigit))
            {
                Errors.SetError(txtNaziv, Properties.Resources.InvalidFormat);
                return false;
            }

            Errors.Clear();
            return true;
        }

        private async void btnSave_Click(object sender, System.EventArgs e)
        {
            if (ValidateInput())
            {
                APIServiceResult<TwoFieldsDto> result;
                var upsertDto = new RoleUpsertDto
                { Naziv = txtNaziv.Text };

                if (Role == null)
                    result = await _apiService.Post<TwoFieldsDto>(upsertDto);
                else
                    result = await _apiService.Update<TwoFieldsDto>(Role.Id, upsertDto);

                if (result.Succeeded)
                {
                    if (Role == null)
                        await frmPredefinedDataMenu.Instance.LoadPredefinedDataCount(Routes.RolesRoute);

                    dlgSuccess.ShowDialog();
                    Close();
                    frmRoles.Instance.RefreshData();
                }

                Close();
                Dispose();
            }
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            Close();
            Dispose();
        }
    }
}