﻿
using Intwenty;
using Intwenty.Data.Entity;
using Microsoft.AspNetCore.Mvc;


namespace IntwentyDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IIntwentyDbAccessService Connection;

        public HomeController(IIntwentyDbAccessService db)
        {
            Connection = db;
        }

        public IActionResult Index()
        {
          
            /*
            Connection.CreateTable<TestEntity>();

            var t = new TestEntity() { Name="Santa Clause", Active = true, Description = "A story about Santa Clause" };
            Connection.Insert(t);
            t = new TestEntity() { Name = "Big Foot", Active = true, Description = "A story about Big Foot" };
            Connection.Insert(t);

            var list = Connection.Get<TestEntity>();
            if (list.Count > 0)
            {
                list[0].Name = "C# ferry";
                list[0].Description = "A story about the C# ferry";
                Connection.Update(list[0]);
            }
            list = Connection.Get<TestEntity>();
            if (list.Count > 0)
            {
                Connection.Delete<TestEntity>(list[0]);
            }

            Connection.CreateTable<TestEntity2>();

            var t2 = new TestEntity2() { Id=1, Name = "Mickey Mouse", Active = false, Description = "A story about mickey mouse" };
            Connection.Insert(t2);
            t2 = new TestEntity2() { Id=2, Name = "Superman", Active = true, Description = "A story about superman" };
            Connection.Insert(t2);

            var list2 = Connection.Get<TestEntity2>();
            if (list2.Count > 0)
            {
                list2[0].Name = "A story about Donald Duck";
                list2[0].Description = "A story about Donald Duck";
                Connection.Update<TestEntity2>(list2[0]);
            }
            list2 = Connection.Get<TestEntity2>();
            if (list2.Count > 0)
            {
                Connection.Delete(list2[0]);
            }
            */

            return View();
        }

      
    }
}
