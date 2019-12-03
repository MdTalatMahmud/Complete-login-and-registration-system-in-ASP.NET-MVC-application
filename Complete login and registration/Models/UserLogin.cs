using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Complete_login_and_registration.Models
{
    public class UserLogin
    {
        [DisplayName("Email ID")]
        [Required(AllowEmptyStrings = false,ErrorMessage = "Email Id Required")]
        public string EmailID { get; set; }

        [Required(AllowEmptyStrings = false,ErrorMessage = "Password Required")]
        public string Password { get; set; }


        [DisplayName("remember be")] 
        public bool RememberMe { get; set; }
    }
}