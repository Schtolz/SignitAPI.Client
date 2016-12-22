using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SignitIntegrationSample.Models
{
    public class SigningGroupViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [Required]
        public string ContactPhone { get; set; }

        [Required]
        public string PostCode { get; set; }

        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }

        [Required]
        public int PaymentPlanId { get; set; }

        [Required]
        public int CommitmentId { get; set; }

        [Required]
        [RegularExpression(@"\d{8}")]
        public string Cvr { get; set; }

        public string PaymentMethodSystemName { get; set; }

        public int LogoHeader { get; set; }
        public int LogoFooter { get; set; }
        public string FooterBackgroundColor { get; set; }
        public string FooterFontColor { get; set; }
        public string FooterText { get; set; }
        public int PageTemplate { get; set; }
        [RegularExpression(@"^(?!http).+", ErrorMessage = "Kan ikke begynde med http")]
        public string Domain { get; set; }
        public bool CustomStylesEnabled { get; set; }
        public int NemIdLoginIntervalDays { get; set; }

        public bool SmsNotification { get; set; }
    }
}