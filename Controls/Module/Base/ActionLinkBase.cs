﻿using Microsoft.AspNetCore.Components;
using Oqtane.Models;
using Oqtane.Modules;
using Oqtane.Modules.Controls;
using Oqtane.Security;
using Oqtane.Shared;
using System.Net;
using System.Text.Json;

namespace MudOqtaneRazorControls.Controls.Module.Base
{
    public partial class ActionLinkBase : LocalizableComponent
    {
        protected string LinkText = string.Empty;
        private int _moduleId = -1;
        private string _path = string.Empty;
        private string _parameters = string.Empty;
        protected string _url = string.Empty;
        private List<Permission> _permissions;
        private bool _editmode = false;
        protected bool Authorized = false;
        private string _classname = "btn btn-primary";
        private string _style = string.Empty;
        protected string IconSpan = string.Empty;

        [Parameter]
        public string Action { get; set; } // required

        [Parameter]
        public string Text { get; set; } // optional - defaults to Action if not specified

        [Parameter]
        public int ModuleId { get; set; } = -1; // optional - allows the link to target a specific moduleid

        [Parameter]
        public string Path { get; set; } = null; // optional - allows the link to target a specific page

        [Parameter]
        public string Parameters { get; set; } // optional - querystring parameters should be in the form of "id=x&name=y"

        [Parameter]
        public Action OnClick { get; set; } = null; // optional - executes a method in the calling component

        [Parameter]
        public SecurityAccessLevel? Security { get; set; } // optional - can be used to explicitly specify SecurityAccessLevel

        [Parameter]
        public string Permissions { get; set; } // deprecated - use PermissionList instead

        [Parameter]
        public List<Permission> PermissionList { get; set; } // optional - can be used to specify permissions

        [Parameter]
        public bool Disabled { get; set; } // optional

        [Parameter]
        public string EditMode { get; set; } // optional - specifies if an authorized user must be in edit mode to see the action - default is false.

        [Parameter]
        public string Class { get; set; } // optional - defaults to primary if not specified

        [Parameter]
        public string Style { get; set; } // optional

        [Parameter]
        public string IconName { get; set; } // optional - specifies an icon for the link - default is no icon

        [Parameter]
        public bool IconOnly { get; set; } // optional - specifies only icon in link

        [Parameter]
        public string ReturnUrl { get; set; } // optional - used to set a url to redirect to


        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(Permissions))
            {
                PermissionList = JsonSerializer.Deserialize<List<Permission>>(Permissions);
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            LinkText = Action;
            if (!string.IsNullOrEmpty(Text))
            {
                LinkText = Text;
            }

            if (IconOnly && !string.IsNullOrEmpty(IconName))
            {
                LinkText = string.Empty;
            }

            _moduleId = ModuleState.ModuleId;
            if (ModuleId != -1)
            {
                _moduleId = ModuleId;
            }

            _path = PageState.Page.Path;
            if (Path != null)
            {
                _path = Path;
            }

            if (!string.IsNullOrEmpty(Parameters))
            {
                _parameters = Parameters;
            }

            if (!string.IsNullOrEmpty(Class))
            {
                _classname = Class;
            }

            if (!string.IsNullOrEmpty(Style))
            {
                _style = Style;
            }

            if (!string.IsNullOrEmpty(EditMode))
            {
                _editmode = bool.Parse(EditMode);
            }

            if (!string.IsNullOrEmpty(IconName))
            {
                if (!IconName.Contains(" "))
                {
                    IconName = "oi oi-" + IconName;
                }
                IconSpan = $"<span class=\"{IconName}\"></span>{(IconOnly ? "" : "&nbsp")}";
            }

            _permissions = (PermissionList == null) ? ModuleState.PermissionList : PermissionList;
            LinkText = Localize(nameof(Text), LinkText);

            _url = EditUrl(_path, _moduleId, Action, _parameters);
            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                _url += ((_url.Contains("?")) ? "&" : "?") + $"returnurl={WebUtility.UrlEncode(ReturnUrl)}";
            }
            Authorized = IsAuthorized();
        }

        private bool IsAuthorized()
        {
            var authorized = false;
            if (PageState.EditMode || !_editmode)
            {
                var security = SecurityAccessLevel.Host;
                if (Security == null)
                {
                    var typename = ModuleState.ModuleType.Replace(Utilities.GetTypeNameLastSegment(ModuleState.ModuleType, 0) + ",", Action + ",");
                    var moduleType = Type.GetType(typename);
                    if (moduleType != null)
                    {
                        var moduleobject = Activator.CreateInstance(moduleType) as IModuleControl;
                        security = moduleobject.SecurityAccessLevel;
                    }
                    else
                    {
                        security = SecurityAccessLevel.Anonymous; // occurs when an action does not have a corresponding module control
                        Class = "btn btn-warning"; // alert developer of missing module comtrol
                    }
                }
                else
                {
                    security = Security.Value;
                }

                switch (security)
                {
                    case SecurityAccessLevel.Anonymous:
                        authorized = true;
                        break;
                    case SecurityAccessLevel.View:
                        authorized = UserSecurity.IsAuthorized(PageState.User, PermissionNames.View, _permissions);
                        break;
                    case SecurityAccessLevel.Edit:
                        authorized = UserSecurity.IsAuthorized(PageState.User, PermissionNames.Edit, _permissions);
                        break;
                    case SecurityAccessLevel.Admin:
                        authorized = UserSecurity.IsAuthorized(PageState.User, RoleNames.Admin);
                        break;
                    case SecurityAccessLevel.Host:
                        authorized = UserSecurity.IsAuthorized(PageState.User, RoleNames.Host);
                        break;
                }
            }

            return authorized;
        }

    }
}
