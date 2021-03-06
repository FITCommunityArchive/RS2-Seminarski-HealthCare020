﻿using System;

namespace HealthCare020.Core.ResourceParameters
{
    public class ZahtevZaPosetuResourceParameters : BaseResourceParameters
    {
        public string PacijentIme { get; set; }
        public string PacijentPrezime { get; set; }
        public DateTime? Datum { get; set; }
        public string BrojTelefonaPosetioca { get; set; }
        public bool NeobradjeneOnly { get; set; } = false;
        public bool ObradjeneOnly { get; set; } = false;

        public bool EagerLoaded { get; set; } = false;
    }
}