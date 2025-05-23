﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Areas.Identity.Data
{
    internal class IntwentyCookieStore : ITicketStore
    {
        private ConcurrentDictionary<string, AuthenticationTicket> mytickets = new();

        public IntwentyCookieStore()
        {
        }

        public Task RemoveAsync(string key)
        {

            if (mytickets.ContainsKey(key))
            {
                mytickets.TryRemove(key, out _);
            }

            return Task.FromResult(0);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
          
            mytickets[key] = ticket;

            return Task.FromResult(false);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
          
            if (mytickets.ContainsKey(key))
            {
                var ticket = mytickets[key];
                return Task.FromResult(ticket);
            }
            else
            {
                return Task.FromResult((AuthenticationTicket)null!);
            }
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString();
            var result = mytickets.TryAdd(key, ticket);

            if (result)
            {
                string username = ticket?.Principal?.Identity?.Name ?? "Unknown";
                return Task.FromResult(key);
            }
            else
            {
                throw new Exception("Failed to add entry to the MySessionStore.");
            }
        }
    }
}
