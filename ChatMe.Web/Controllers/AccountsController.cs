﻿using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using ChatMe.Web.Models;
using AutoMapper;
using ChatMe.BussinessLogic.DTO;
using ChatMe.BussinessLogic.Services.Abstract;

namespace ChatMe.Web.Controllers
{
    public class AccountsController : Controller
    {
        private IAccountService accountService;

        public AccountsController(IAccountService accountService) {
            this.accountService = accountService;
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid) {
                Mapper.Initialize(cfg => cfg.CreateMap<RegisterViewModel, RegistrationInfoDTO>());
                var regInfo = Mapper.Map<RegistrationInfoDTO>(model);
                var result = await accountService.CreateUser(regInfo);

                if (result.Succeeded) {
                    return RedirectToAction("Login", "Account");
                } else {
                    foreach (var error in result.Errors) {
                        ModelState.AddModelError("", error);
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public ViewResult Login(string returnUrl)
        {
            ViewBag.returnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {

            if (ModelState.IsValid) {
                Mapper.Initialize(cfg => cfg.CreateMap<LoginViewModel, LoginDTO>());
                var loginData = Mapper.Map<LoginDTO>(model);

                var isSuccessLogin = await accountService.Login(loginData);

                if (isSuccessLogin) {
                    if (string.IsNullOrEmpty(returnUrl)) {
                        return RedirectToAction("Index", "Home");
                    } else {
                        return Redirect(returnUrl);
                    }
                } else {
                    ModelState.AddModelError("", "Invalid login or password");                    
                }
            }

            ViewBag.returnUrl = returnUrl;
            return View(model);
        }

        public ActionResult Logout()
        {
            HttpContext.GetOwinContext();
            accountService.Logout();
            return RedirectToAction("Login");
        }
    }
}