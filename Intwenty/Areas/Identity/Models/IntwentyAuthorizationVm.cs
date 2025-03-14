using Intwenty.Areas.Identity.Entity;
using Intwenty.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Areas.Identity.Models
{
  
    public class IntwentyAuthorizationVm
    {
     
        public IntwentyAuthorizationVm(IntwentyAuthorization entity)
        {
            Id = entity.Id;
            AuthorizationType = entity.AuthorizationType;
            AuthorizationId = entity.AuthorizationId;
            AuthorizationName = entity.AuthorizationName;
            AuthorizationNormalizedName = entity.AuthorizationNormalizedName;
            UserId = entity.UserId;
            UserName = entity.UserName;
            ProductId = entity.ProductId;
            OrganizationId = entity.OrganizationId;
            OrganizationName = entity.OrganizationName;
            DenyAuthorization = entity.DenyAuthorization;

        }

        public IntwentyAuthorizationVm()
        {
           
        }

    

        public int Id { get; set; }
        public string ProductId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string AuthorizationId { get; set; }
        public string AuthorizationName { get; set; }
        public string AuthorizationNormalizedName { get; set; }
        public string AuthorizationType { get; set; }
        public bool DenyAuthorization { get; set; }


        public bool IsSystemAuthorization
        {
            get { return AuthorizationType == "SYSTEM"; }
        }

        public bool IsApplicationAuthorization
        {
            get { return AuthorizationType == "APPLICATION"; }
        }

        public bool IsProductAuthorization
        {
            get { return AuthorizationType == "ROLE"; }
        }

        public bool IsViewAuthorization
        {
            get { return AuthorizationType == "VIEW"; }
        }


    }
}
