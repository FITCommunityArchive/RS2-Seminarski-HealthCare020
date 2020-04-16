﻿namespace HealthCare020.Core.ResourceParameters
{
    public class LicniPodaciResourceParameters:BaseResourceParameters
    {
        public int? Id { get; set; }

        public string Ime { get; set; }

        public string Prezime { get; set; }

        public string Adresa { get; set; }

        public char Pol { get; set; }

        public string BrojTelefona { get; set; }

        public string Grad { get; set; }

        public bool EagerLoaded { get; set; } = false;


    }
}