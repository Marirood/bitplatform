﻿using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Owin.Exceptions;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.DataAccess.Contracts;
using BitChangeSetManager.Model;
using IdentityServer3.Core.Models;
using System;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerUserService : UserService
    {
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public override async Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context)
        {
            string username = context.UserName;
            string password = context.Password;

            if (string.IsNullOrEmpty(username))
                throw new ArgumentException(nameof(username));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException(nameof(password));

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder sBuilder = new StringBuilder();

                foreach (byte d in data)
                {
                    sBuilder.Append(d.ToString("x2"));
                }

                password = sBuilder.ToString();
            }

            username = username.ToLower();

            User user = null;

            user = await (await UsersRepository.GetAllAsync(CancellationToken.None))
                 .SingleOrDefaultAsync(u => u.UserName.ToLower() == username && u.Password == password);

            if (user == null)
                throw new DomainLogicException("LoginFailed");

            return user.Id.ToString();
        }

        public override async Task<bool> UserIsActiveAsync(IsActiveContext context, string userId)
        {
            Guid userIdAsGuid = Guid.Parse(userId);

            return await (await UsersRepository.GetAllAsync(CancellationToken.None))
                 .AnyAsync(u => u.Id == userIdAsGuid);
        }
    }
}