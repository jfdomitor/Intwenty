using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Data;
using Intwenty.Model.Dto;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Intwenty.Services;
using Intwenty.Interface;

namespace Intwenty.Areas.Identity.Pages.Account.Manage
{
    public partial class GroupsModel : PageModel
    {
        private readonly IntwentyUserManager _userManager;
        private readonly IntwentySettings _settings;
        private readonly IIntwentyEventService _eventservice;

        public GroupsModel(
            IntwentyUserManager userManager,
            IOptions<IntwentySettings> settings,
            IIntwentyEventService eventservice)
        {
            _userManager = userManager;
            _settings = settings.Value;
            _eventservice = eventservice;
        }

        public class SetMembershipModel
        {
            public string UserId { get; set; }
            public string Status { get; set; }
            public string GroupId { get; set; }
        }

        public class RenameGroupModel
        {
            public string GroupName { get; set; }
            public string NewName { get; set; }
            public string GroupId { get; set; }
        }

        public class InviteToGroupModel
        {
            public string GroupName { get; set; }
            public string Email { get; set; }
            public string GroupId { get; set; }
        }


        public class CreateOrJoinGroupModel
        {
            public string GroupName { get; set; }
        }

        public class LeaveGroupModel
        {
            public string GroupId { get; set; }

            public string UserId { get; set; }
        }

        public class MyGroupConnections
        {
            public List<IntwentyUserGroupVm> MyGroups  { get; set; }

            public List<GroupMemberShip> MyGroupsMembers { get; set; }

            public MyGroupConnections()
            {
                MyGroups = new List<IntwentyUserGroupVm>();
                MyGroupsMembers = new List<GroupMemberShip>();
            }
        }

        public class GroupMemberShip
        {
            public string GroupId { get; set; }

            public string GroupName { get; set; }

            public List<IntwentyUserGroupVm> Members { get; set; }

            public GroupMemberShip()
            {
                Members = new List<IntwentyUserGroupVm>();
            }
        }

        public class IntwentyUserGroupVm : IntwentyUserProductGroup
        {
            public string UserRole { get; set; }

            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanInviteToGroup { get; set; }

            /// <summary>
            /// If curent user is waiting member
            /// </summary>
            public bool CanAcceptInvitation { get; set; }


            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanRemoveInvitation { get; set; }

            /// <summary>
            /// If curent user is member
            /// </summary>
            public bool CanLeave{ get; set; }

            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanRemoveMember { get; set; }

            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanRemoveGroup { get; set; }

            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanAcceptJoinRequest { get; set; }

            /// <summary>
            /// If curent user is owner
            /// </summary>
            public bool CanRenameGroup { get; set; }

            public IntwentyUserGroupVm(IntwentyUserProductGroup model)
            {
                this.GroupId = model.GroupId;
                this.GroupName = model.GroupName;
                this.UserId = model.UserId;
                this.UserName = model.UserName;
                this.Id = model.Id;
                this.MembershipStatus = model.MembershipStatus;
                this.MembershipType = model.MembershipType;
                if (model.MembershipType == "GROUPADMIN")
                    this.UserRole = "Owner";
                if (model.MembershipType == "GROUPMEMBER" && model.MembershipStatus == "ACCEPTED")
                    this.UserRole = "Member";
                if (model.MembershipType == "GROUPMEMBER" && model.MembershipStatus == "REQUESTED")
                    this.UserRole = "Requested to join";
                if (model.MembershipType == "GROUPMEMBER" && model.MembershipStatus == "INVITED")
                    this.UserRole = "Invited member";


            }

        }

        public IActionResult OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnGetLoadGroups()
        {
            var result = new MyGroupConnections();
           
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var usergroups = await _userManager.GetUserGroups(user);
                foreach (var ug in usergroups)
                {
                    var vm = new IntwentyUserGroupVm(ug);
                    if (ug.UserId == user.Id && ug.MembershipType == "GROUPADMIN")
                    {
                        vm.CanInviteToGroup = true;
                        vm.CanRenameGroup = true;
                    }

                    if (ug.UserId == user.Id && ug.MembershipType == "GROUPMEMBER")
                    {
                        vm.CanLeave = true;
                    }


                    if (ug.UserId == user.Id && ug.MembershipType == "GROUPMEMBER" && ug.MembershipStatus == "INVITED")
                    {
                        vm.CanAcceptInvitation = true;
                    }
                    
                    result.MyGroups.Add(vm);
                    result.MyGroupsMembers.Add(new GroupMemberShip() { GroupId = ug.GroupId, GroupName = ug.GroupName }); 
                }
                foreach (var group in result.MyGroupsMembers) 
                {
                    var groupmembers = await _userManager.GetGroupMembers(new IntwentyProductGroup() { Id = group.GroupId, Name=group.GroupName });
                    foreach (var gm in groupmembers)
                    {
                        group.Members.Add(new IntwentyUserGroupVm(gm));
                    }
                }

                foreach (var g in result.MyGroupsMembers)
                {
                    if (result.MyGroups.Exists(p => p.GroupId == g.GroupId && p.UserId == user.Id && p.MembershipType == "GROUPADMIN"))
                    {
                        foreach (var member in g.Members.Where(p => p.MembershipType == "GROUPMEMBER" && p.UserId != user.Id).ToList())
                        {
                            if (member.MembershipStatus == "REQUESTED")
                            {
                                member.CanRemoveMember = true;
                                member.CanAcceptJoinRequest = true;
                            }

                            if (member.MembershipStatus == "INVITED")
                            {
                                member.CanRemoveInvitation = true;
                            }

                            if (member.MembershipStatus == "ACCEPTED")
                                member.CanRemoveMember = true;

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                var jres = new JsonResult("There was an error when adding a group.");
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult(result);
        }

       
        public async Task<IActionResult> OnPostMembershipChange([FromBody] SetMembershipModel model)
        {

            var user =  await _userManager.FindByIdAsync(model.UserId);
            if (user != null)
            {
                if (model.Status == "REMOVE")
                {
                    var t = await _userManager.RemoveFromGroupAsync(user.Id, model.GroupId);
                    if (t == IdentityResult.Success)
                    {
                        try
                        {
                            var group = await _userManager.GetGroupByIdAsync(model.GroupId);
                            var me = await _userManager.GetUserAsync(User);
                            await _eventservice.UserRemovedFromGroup(new UserRemovedFromGroupData() { GroupName = group.Name, SenderUserName = me.UserName, ReceiverUserName = user.UserName });
                        }
                        catch { }
                   }
                }
                else
                {
                    var group = await _userManager.GetGroupByIdAsync(model.GroupId);
                    await _userManager.UpdateGroupMembershipAsync(user, group, model.Status);

                }
            }

            return new JsonResult("{}");
      
        }

        
       public async Task<IActionResult> OnPostInviteToGroup([FromBody] InviteToGroupModel model)
       {
            try
            {

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var group = await _userManager.GetGroupByIdAsync(model.GroupId);
                    if (group != null)
                    {
                        var members = await _userManager.GetGroupMembers(group);
                        if (members != null)
                        {
                            if (members.Exists(p => p.UserName.ToUpper() == model.Email.ToUpper()))
                                throw new InvalidOperationException("The user " +  model.Email + " is already a member of " + group.Name);
                        }
                        var t = await _userManager.AddGroupMemberAsync(user, group, "GROUPMEMBER", "INVITED");
                        if (t == IdentityResult.Success)
                        {
                            try
                            {
                                var me = await _userManager.GetUserAsync(User);
                                await _eventservice.UserInvitedToGroup(new UserInvitedData() { GroupName = group.Name, SenderUserName = me.UserName, ReceiverUserName = user.UserName });
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(model.Email + " is not a user of this service. Contact the user and ask him/her to create an account first.");
                }

            }
            catch (Exception ex)
            {
                var jres = new JsonResult("There was an error when inviting a user a group.");
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult("{}");

       }

        public async Task<IActionResult> OnPostRenameGroup([FromBody] RenameGroupModel model)
        {

            await _userManager.ChangeGroupNameAsync(model.GroupId, model.NewName);
            return new JsonResult("{}");

        }


        public async Task<IActionResult> OnPostCreateGroup([FromBody] CreateOrJoinGroupModel model)
        {

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (await _userManager.GroupExists(model.GroupName))
                    throw new InvalidOperationException("The group already exists, type another name.");

                var group = await _userManager.AddGroupAsync(model.GroupName);
                await _userManager.AddGroupMemberAsync(user, group, "GROUPADMIN", "ACCEPTED");

            }
            catch (Exception ex)
            {
                var jres = new JsonResult("There was an error when creating a group.");
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult("{}");

        }

        public async Task<IActionResult> OnPostJoinGroup([FromBody] CreateOrJoinGroupModel model)
        {

            try
            {
                var user = await _userManager.GetUserAsync(User);
               
                if (!await _userManager.GroupExists(model.GroupName))
                    throw new InvalidOperationException("The group does not exists, type another groupname.");

                var usergroups = await _userManager.GetUserGroups(user);
                if (usergroups.Exists(p=> p.GroupName== model.GroupName))
                    throw new InvalidOperationException("You are already a member or admin of this group.");

                var group = await _userManager.GetGroupByNameAsync(model.GroupName);
                var t = await _userManager.AddGroupMemberAsync(user, group, "GROUPMEMBER", "REQUESTED");
                if (t == IdentityResult.Success)
                {
                    try
                    {
                        var members = await _userManager.GetGroupMembers(group);
                        var owner = members.Find(p => p.MembershipType == "GROUPADMIN");
                        await _eventservice.UserInvitedToGroup(new UserInvitedData() { GroupName = group.Name, SenderUserName = user.UserName, ReceiverUserName = owner.UserName });
                    }
                    catch { }
                }

            }
            catch (Exception ex)
            {
                var jres = new JsonResult("There was an error when joining a group.");
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult("{}");

        }

        public async Task<IActionResult> OnPostLeaveGroup([FromBody] LeaveGroupModel model)
        {

            try
            {
               await  _userManager.RemoveFromGroupAsync(model.UserId, model.GroupId);
               
            }
            catch (Exception ex)
            {
                var jres = new JsonResult("There was an error when leaving a group.");
                jres.StatusCode = 500;
                return jres;
            }

            return new JsonResult("{}");

        }


    }
}
