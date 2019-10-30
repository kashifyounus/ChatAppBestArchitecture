﻿using ChatMe.BussinessLogic.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatMe.DataAccess.Entities;
using ChatMe.BussinessLogic.DTO;
using ChatMe.DataAccess.Interfaces;
using Microsoft.AspNet.Identity;
using System.IO;
using System.Configuration;

namespace ChatMe.BussinessLogic.Services
{
    public class UserService : IUserService
    {
        private IUnitOfWork db;

        public UserService(IUnitOfWork unitOfWork) {
            this.db = unitOfWork;
        }

        public IEnumerable<UserInfoDTO> GetAll() {
            var usersData = db.Users.Users
                .Select(u => new UserInfoDTO {
                    Id = u.Id,
                    AvatarFilename = u.UserInfo.AvatarFilename,
                    UserName = u.UserName,
                    FirstName = u.UserInfo.FirstName,
                    LastName = u.UserInfo.LastName,
                    IsOnline = u.IsOnline
                });
            return usersData;
        }

        public string GetUserDisplayName(User user) {
            if (user == null) {
                return "Empty";
            }

            if (user.UserInfo.FirstName != null && user.UserInfo.LastName != null) {
                return $"{user.UserInfo.FirstName} {user.UserInfo.LastName}";
            } else {
                return user.UserName;
            }
        }

        public UserProfileDTO GetUserProfile(string userName, string currectUserId) {

            var role = new Role()
            {
                Name = "user"
            };
            var rr = db.Roles.Create(role);

            var user = db.Users.FindByName(userName);
            var me = db.Users.FindById(currectUserId);

            if (user == null) {
                return null;
            }

            var userProfile = new UserProfileDTO {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.UserInfo.FirstName,
                LastName = user.UserInfo.LastName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Phone = user.UserInfo.Phone,
                Skype = user.UserInfo.Skype,
                AboutMe = user.UserInfo.AboutMe,
                AvatarFilename = user.UserInfo.AvatarFilename,
                IsOwner = user.Id == me.Id,
                IsFollowing = user.Followers.Any(u => u.Id == currectUserId),
                IsOnline = user.IsOnline
            };

            return userProfile;
        }

        public UserSettingsDTO GetUserSettings(string userId) {
            var me = db.Users.FindById(userId);
            var data = new UserSettingsDTO {
                Id = me.Id,
                Email = me.Email,
                FirstName = me.UserInfo.FirstName,
                LastName = me.UserInfo.LastName,
                AboutMe = me.UserInfo.AboutMe,
                AvatarFilename = me.UserInfo.AvatarFilename,
                Phone = me.UserInfo.Phone,
                Skype = me.UserInfo.Skype
            };

            return data;
        }

        public async Task<ChangingSettingsResult> ChangeUserSettings(UserSettingsDTO settingsData, Func<string, string> pathResolver) {
            var me = db.Users.FindById(settingsData.Id);
            var result = new ChangingSettingsResult();
            result.Settings = settingsData;

            var isPassValid = db.Users.CheckPassword(me, settingsData.Password);
            if (!isPassValid) {
                result.Errors.Add("Invalid password");
                result.Succeeded = false;
                settingsData.Password = "";
                return result;
            }

            if (!string.IsNullOrEmpty(settingsData.NewPassword)) {
                if (settingsData.NewPassword != settingsData.NewPasswordConfirmation) {
                    settingsData.NewPassword = "";
                    settingsData.NewPasswordConfirmation = "";
                    settingsData.Password = "";

                    result.Errors.Add("Passwords don't match");
                    result.Succeeded = false;
                    return result;
                }

                await db.Users.ChangePasswordAsync(me.Id, settingsData.Password, settingsData.NewPassword);
            }

            me.UserInfo.FirstName = settingsData.FirstName;
            me.UserInfo.LastName = settingsData.LastName;
            me.UserInfo.Phone = settingsData.Phone;
            me.UserInfo.Skype = settingsData.Skype;
            me.Email = settingsData.Email;
            me.UserInfo.AboutMe = settingsData.AboutMe;

            if (settingsData.Avatar != null) {
                var extension = settingsData.Avatar.FileName.Split('.').Last();
                var filename = $"{me.Id}.{extension}";
                var savePath = Path.Combine(ConfigurationManager.AppSettings["avatarPath"].ToString(),
                    filename);
                settingsData.Avatar.SaveAs(pathResolver(savePath));

                me.UserInfo.AvatarFilename = filename;
                me.UserInfo.AvatarMimeType = settingsData.Avatar.ContentType;
            }

            await db.Users.UpdateAsync(me);
            await db.SaveChangesAsync();

            result.Succeeded = true;
            return result;
        }

        public IEnumerable<UserInfoDTO> GetAllExceptMe(string userId) {
            var allUsers = GetAll();
            return allUsers.Where(u => u.Id != userId);
        }
    }
}
