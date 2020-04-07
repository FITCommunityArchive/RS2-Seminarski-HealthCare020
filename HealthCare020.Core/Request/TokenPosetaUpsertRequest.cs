﻿using System.ComponentModel.DataAnnotations;

namespace HealthCare020.Core.Request
{
    public class TokenPosetaUpsertRequest
    {
        [Required(ErrorMessage = "Value is required field")]
        [StringLength(maximumLength: 9, MinimumLength = 5,ErrorMessage = "Vrijednost tokena mora biti duzine izmedju 5 i 9 karaktera")]
        [DataType(DataType.Text)]
        public string Value { get; set; }
    }
}