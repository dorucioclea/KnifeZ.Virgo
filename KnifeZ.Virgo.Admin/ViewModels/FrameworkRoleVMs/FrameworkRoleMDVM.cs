using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeZ.Virgo.Core;
using KnifeZ.Virgo.Mvc.Admin.ViewModels.FrameworkMenuVMs;
using KnifeZ.Virgo.Core.Extensions;

namespace KnifeZ.Virgo.Mvc.Admin.ViewModels.FrameworkRoleVMs
{
    public class FrameworkRoleMDVM : BaseCRUDVM<FrameworkRole>
    {
        public FrameworkMenuListVM ListVM { get; set; }

        public FrameworkRoleMDVM()
        {
            ListVM = new FrameworkMenuListVM();
        }

        protected override void InitVM()
        {
            ListVM.CopyContext(this);
            ListVM.Searcher.RoleID = Entity.ID;
        }

        public async Task<bool> DoChangeAsync()
        {
            var all = FC.Where(x => x.Key.StartsWith("menu_")).ToList();
            List<Guid> AllowedMenuIds = all.Where(x => x.Value.ToString() == "1").Select(x=> Guid.Parse(x.Key.Replace("menu_",""))).ToList();
            var torem = AllowedMenuIds.Distinct();
            var oldIDs = DC.Set<FunctionPrivilege>().Where(x => x.RoleId == Entity.ID).Select(x => x.ID).ToList();

            foreach (var oldid in oldIDs)
            {
                FunctionPrivilege fp = new FunctionPrivilege { ID = oldid };
                DC.Set<FunctionPrivilege>().Attach(fp);
                DC.DeleteEntity(fp);
            }
            foreach (var menuid in AllowedMenuIds)
            {
                FunctionPrivilege fp = new FunctionPrivilege();
                fp.MenuItemId = menuid;
                fp.RoleId = Entity.ID;
                fp.UserId = null;
                fp.Allowed = true;
                DC.Set<FunctionPrivilege>().Add(fp);
            }
            await DC.SaveChangesAsync();
            var userids = DC.Set<FrameworkUserRole>().Where(x => x.RoleId == Entity.ID).Select(x => x.UserId.ToString()).ToArray();
            await LoginUserInfo.RemoveUserCache(userids);
            return true;
        }

    }
}