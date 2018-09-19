using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Database.Models
{
    public class Validation
    {
        //# ID_, SECURITY_ID, VALIDATION_TYPE, LAST_VALIDATION
        [Column(Storage ="ID_")]
        public int Id { get; set; }

        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }

        [Column(Storage = "VALIDATION_TYPE")]
        public int ValitationId { get; set; }

        [Column(Storage = "LAST_VALIDATION")]
        public DateTime LastValidationDateTime { get; set; }

        [Column(Storage = "IS_VALID")]
        public int IsValid { get; set; }

        public override string ToString()
        {
            return $"Secid:{SecurityId}_Type:{ValitationId}_Date:{LastValidationDateTime}_IsValid:{IsValid}";
        }

    }
}
