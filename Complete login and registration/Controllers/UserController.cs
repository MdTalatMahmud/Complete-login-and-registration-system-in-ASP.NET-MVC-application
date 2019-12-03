using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Complete_login_and_registration.Models;

namespace Complete_login_and_registration.Controllers
{
    public class UserController : Controller
    {
        //registration action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        //registration POST action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVarified,ActivationCode")] User user)
        {
            bool Status = false;
            string Message = "";
            //model validation
            if (ModelState.IsValid)
            {
                #region //Email is already exist

                var isExist = IsEmailExist(user.EmailID);
                if (isExist==true)
                {
                    ModelState.AddModelError("EmailExist","Email already exist");
                    return View(user);
                }

                #endregion

                #region //generate activation code

                user.ActivationCode = Guid.NewGuid();


                #endregion

                #region //password hashing

                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);

                #endregion

                user.IsEmailVarified = false;//firstly we made IsEmailVarified is flase

                #region //save to database

                using (MyDatabaseEntities dc = new MyDatabaseEntities())
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();

                    //send email to user
                    SendVarificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                    Message = "Registration done! Account activation link has been sent to your email id: " +
                              user.EmailID;
                    Status = true;
                }
                

                #endregion

            }
            else
            {
                Message = "Invalid Request";
            }

            ViewBag.Message = Message;
            ViewBag.Status = Status;
            return View(user);
        }
        //verify email
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (MyDatabaseEntities dc=new MyDatabaseEntities())
            {
                dc.Configuration.ValidateOnSaveEnabled = false;
                var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (v!=null)
                {
                    v.IsEmailVarified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "invalid Request";
                }
            }

            ViewBag.Status = Status;
            return View();
        }
        //verify email link

        //login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        //login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login,string ReturnUrl="")
        {
            string message = "";
            using (MyDatabaseEntities dc=new MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID==login.EmailID).FirstOrDefault();
                if (v!=null)
                {
                    if (string.Compare(Crypto.Hash(login.Password),v.Password ) == 0)
                    {
                        int timeout = login.RememberMe ? 525600 : 20;//525600 min=1year
                        var ticket = new FormsAuthenticationTicket(login.EmailID, login.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie =new HttpCookie(FormsAuthentication.FormsCookieName,encrypted);
                        cookie.Expires=DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);

                        if (Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        message = "invalid credential provided ";

                    }
                }
                else
                {
                    message = "invalid credential provided";
                }
            }
            ViewBag.message = message;
            return View();
        }
        //logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");

        }
        

        //[NonAction] means method is not an action
        [NonAction]
        public bool IsEmailExist(string emailID)
        {
            using (MyDatabaseEntities dc=new MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                return v != null;
            }
        }

        [NonAction]
        public void SendVarificationLinkEmail(string emailID,string activationCode)
        {
//            var scheme = Request.Url.Scheme;
//            var host = Request.Url.Host;
//            var port = Request.Url.Port;
            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);
            var fromEmail = new MailAddress("dotnettest@gmail.com", "Dotnet Awesome");
            var toEmail=new MailAddress(emailID);
            var fromEmailPassword = "talat1511";
            string subject = "your account is successfully created";

            string body = "<br></br>we are exited to tell you that you have successfully created your account. please click on the below link to verify your account"+"<a href='"+link+"'>"+link+"</a>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };
            //587
            //            using (var message = new MailMessage(fromEmail, toEmail)
            //            {
            //                Subject = subject,
            //                Body = body,
            //                IsBodyHtml = true
            //            }) smtp.Send(message);

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            }) smtp.Send(message);

            //smtp.Send(message);


        }

    }
}