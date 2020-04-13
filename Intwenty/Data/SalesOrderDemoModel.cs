﻿using Intwenty.Data.DBAccess;
using Intwenty.Data.DBAccess.Helpers;
using Intwenty.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Intwenty.Data
{
    public static class SalesOrderDemoModel
    {
        public static void Seed(IServiceProvider provider)
        {
            var Settings = provider.GetRequiredService<IOptions<SystemSettings>>();

            if (!Settings.Value.IsDevelopment)
                return;

            if (!Settings.Value.IntwentyDBMSIsNoSQL)
                SeedSql(provider);
            else
                SeedNoSql(provider);
        }

        public static void SeedNoSql(IServiceProvider provider)
        {
            var Settings = provider.GetRequiredService<IOptions<SystemSettings>>();
            var Connections = provider.GetRequiredService<IOptions<ConnectionStrings>>();

            var nosqlclient = new MongoDB.Driver.MongoClient(Connections.Value.IntwentyConnection);
            var database = nosqlclient.GetDatabase("Intwenty");

            

            var apps = database.GetCollection<ApplicationItem>("sysmodel_ApplicationItem");
            if (apps == null)
            {
                database.CreateCollection("sysmodel_ApplicationItem");
                apps = database.GetCollection<ApplicationItem>("sysmodel_ApplicationItem");
            }

            var valuedomain = database.GetCollection<ValueDomainItem>("sysmodel_ValueDomainItem");
            if (valuedomain == null)
            {
                database.CreateCollection("sysmodel_ValueDomainItem");
                valuedomain = database.GetCollection<ValueDomainItem>("sysmodel_ValueDomainItem");
            }

            var menu = database.GetCollection<MenuItem>("sysmodel_MenuItem");
            if (menu == null)
            {
                database.CreateCollection("sysmodel_MenuItem");
                menu = database.GetCollection<MenuItem>("sysmodel_MenuItem");
            }

            var dbitem = database.GetCollection<DatabaseItem>("sysmodel_DatabaseItem");
            if (dbitem == null)
            {
                database.CreateCollection("sysmodel_DatabaseItem");
                dbitem = database.GetCollection<DatabaseItem>("sysmodel_DatabaseItem");
            }

            var uiitem = database.GetCollection<UserInterfaceItem>("sysmodel_UserInterfaceItem");
            if (uiitem == null)
            {
                database.CreateCollection("sysmodel_UserInterfaceItem");
                uiitem = database.GetCollection<UserInterfaceItem>("sysmodel_UserInterfaceItem");
            }

            var dataview = database.GetCollection<DataViewItem>("sysmodel_DataViewItem");
            if (dataview == null)
            {
                database.CreateCollection("sysmodel_DataViewItem");
                dataview = database.GetCollection<DataViewItem>("sysmodel_DataViewItem");
            }

            var systemid = database.GetCollection<SystemID>("sysdata_SystemId");
            if (systemid == null)
            {
                database.CreateCollection("sysdata_SystemId");
                systemid = database.GetCollection<SystemID>("sysdata_SystemId");
            }

            var informationstatus = database.GetCollection<InformationStatus>("sysdata_InformationStatus");
            if (informationstatus == null)
            {
                database.CreateCollection("sysdata_InformationStatus");
                informationstatus = database.GetCollection<InformationStatus>("sysdata_InformationStatus");
            }

            var eventlog = database.GetCollection<EventLog>("sysdata_EventLog");
            if (eventlog == null)
            {
                database.CreateCollection("sysdata_EventLog");
                eventlog = database.GetCollection<EventLog>("sysdata_EventLog");
            }

            if (Settings.Value.ReCreateModelOnStartUp)
            {
                apps.DeleteMany("{}");
                valuedomain.DeleteMany("{}");
                menu.DeleteMany("{}");
                dbitem.DeleteMany("{}");
                uiitem.DeleteMany("{}");
                dataview.DeleteMany("{}");
            }

            apps.InsertOne(new ApplicationItem() { Id = 10, Description = "An app for managing customers", MetaCode = "CUSTOMER", Title = "Customer", DbName = "Customer", IsHierarchicalApplication = false, UseVersioning = false });
            apps.InsertOne(new ApplicationItem() { Id = 20, Description = "An app for managing items", MetaCode = "ITEM", Title = "Item", DbName = "Item", IsHierarchicalApplication = false, UseVersioning = false });
            apps.InsertOne(new ApplicationItem() { Id = 30, Description = "An app for managing sales orders", MetaCode = "SALESORDER", Title = "Sales Order", DbName = "SalesHeader", IsHierarchicalApplication = false, UseVersioning = false });
           
            menu.InsertOne(new MenuItem() { Id=10, AppMetaCode = "", MetaType = "MAINMENU", MetaCode = "SYSMENU", ParentMetaCode = "ROOT", Title = "Menu", OrderNo = 1, Action = "", Controller = "" });
            menu.InsertOne(new MenuItem() { Id=20, AppMetaCode = "CUSTOMER", MetaType = "MENUITEM", MetaCode = "M_CUST", ParentMetaCode = "SYSMENU", Title = "Customer", OrderNo = 1, Action = "", Controller = "" });
            menu.InsertOne(new MenuItem() { Id=30, AppMetaCode = "ITEM", MetaType = "MENUITEM", MetaCode = "M_ITEM", ParentMetaCode = "SYSMENU", Title = "Item", OrderNo = 10, Action = "", Controller = "" });
            menu.InsertOne(new MenuItem() { Id=40, AppMetaCode = "SALESORDER", MetaType = "MENUITEM", MetaCode = "M_SORD", ParentMetaCode = "SYSMENU", Title = "Sales Order", OrderNo = 20, Action = "", Controller = "" });

            valuedomain.InsertOne(new ValueDomainItem() { Id=50, DomainName = "ITEMCATEGORY", Code = "A1", Value = "Primary" });
            valuedomain.InsertOne(new ValueDomainItem() { Id=60, DomainName = "ITEMCATEGORY", Code = "A2", Value = "Secondary" });


            #region customer
            //APPLICATION CUSTOMER
            //--------------------
            //DATABASE - MAINTABLE
            dbitem.InsertOne(new DatabaseItem() { Id=70, AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERID", DbName = "CustomerId", ParentMetaCode = "ROOT", DataType = "STRING", Mandatory = true });
            dbitem.InsertOne(new DatabaseItem() { Id=80, AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERNAME", DbName = "CustomerName", ParentMetaCode = "ROOT", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=90, AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERINFO", DbName = "CustomerInfo", ParentMetaCode = "ROOT", DataType = "TEXT" });

            //UI
            uiitem.InsertOne(new UserInterfaceItem() { Id=100, AppMetaCode = "CUSTOMER", MetaType = "SECTION", MetaCode = "MAINSECTION", DataMetaCode = "", Title = "Sales Header", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=110, AppMetaCode = "CUSTOMER", MetaType = "PANEL", MetaCode = "CUSTPNL1", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=120, AppMetaCode = "CUSTOMER", MetaType = "TEXTBOX", MetaCode = "TB_CUSTID", DataMetaCode = "CUSTOMERID", Title = "Customer ID", ParentMetaCode = "CUSTPNL1", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=130, AppMetaCode = "CUSTOMER", MetaType = "TEXTBOX", MetaCode = "TB_CUSTNAME", DataMetaCode = "CUSTOMERNAME", Title = "Customer Name", ParentMetaCode = "CUSTPNL1", RowOrder = 2, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=140, AppMetaCode = "CUSTOMER", MetaType = "PANEL", MetaCode = "CUSTPNL2", DataMetaCode = "", Title = "Information", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 2 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=150, AppMetaCode = "CUSTOMER", MetaType = "TEXTAREA", MetaCode = "TA_CUSTINFO", DataMetaCode = "CUSTOMERINFO", Title = "Customer Notes", ParentMetaCode = "CUSTPNL2", RowOrder = 3, ColumnOrder = 1 });

            //LISTVIEW
            uiitem.InsertOne(new UserInterfaceItem() { Id=160, AppMetaCode = "CUSTOMER", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Customer List", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=170, AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=180, AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_CUSTID", DataMetaCode = "CUSTOMERID", Title = "Customer ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=190, AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_CUSTNAME", DataMetaCode = "CUSTOMERNAME", Title = "Customer Name", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });
            #endregion

            #region item
            //APPLICATION ITEM
            //---------------------
            //DATABASE - MAINTABLE
            dbitem.InsertOne(new DatabaseItem() { Id=200, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "ITEMID", DbName = "ItemId", ParentMetaCode = "ROOT", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=210, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "NAME", DbName = "ItemName", ParentMetaCode = "ROOT", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=220, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "CATCODE", DbName = "ItemCategoryCode", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "VALUEDOMAIN.ITEMCATEGORY.CODE" });
            dbitem.InsertOne(new DatabaseItem() { Id=230, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "CATVALUE", DbName = "ItemCategoryValue", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "VALUEDOMAIN.ITEMCATEGORY.VALUE" });
            dbitem.InsertOne(new DatabaseItem() { Id=240, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "MODIFIED", DbName = "Modified", ParentMetaCode = "ROOT", DataType = "DATETIME" });
            dbitem.InsertOne(new DatabaseItem() { Id=250, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "ACTIVE", DbName = "Active", ParentMetaCode = "ROOT", DataType = "BOOLEAN" });
            dbitem.InsertOne(new DatabaseItem() { Id=260, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "PURCHASEPRICE", DbName = "PurchasePrice", ParentMetaCode = "ROOT", DataType = "2DECIMAL" });
            dbitem.InsertOne(new DatabaseItem() { Id=270, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "VENDORCODE", DbName = "VendorCode", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "APP.VENDOR.VENDORID" });
            dbitem.InsertOne(new DatabaseItem() { Id=280, AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "VENDORTXT", DbName = "VendorName", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "APP.VENDOR.VENDORNAME" });

            //UI
            uiitem.InsertOne(new UserInterfaceItem() { Id=290, AppMetaCode = "ITEM", MetaType = "SECTION", MetaCode = "MAINSECTION", DataMetaCode = "", Title = "Basics", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=300, AppMetaCode = "ITEM", MetaType = "PANEL", MetaCode = "ITMPNL_A", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=310, AppMetaCode = "ITEM", MetaType = "TEXTBOX", MetaCode = "TB_ITID", DataMetaCode = "ITEMID", Title = "Item ID", ParentMetaCode = "ITMPNL_A", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=320, AppMetaCode = "ITEM", MetaType = "TEXTBOX", MetaCode = "TB_ITNAME", DataMetaCode = "NAME", Title = "Item Name", ParentMetaCode = "ITMPNL_A", RowOrder = 2, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=330, AppMetaCode = "ITEM", MetaType = "DATEPICKER", MetaCode = "DP_MOD", DataMetaCode = "MODIFIED", Title = "Modified Date", ParentMetaCode = "ITMPNL_A", RowOrder = 3, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=340, AppMetaCode = "ITEM", MetaType = "CHECKBOX", MetaCode = "CB_ACTIVE", DataMetaCode = "ACTIVE", Title = "Is Active", ParentMetaCode = "ITMPNL_A", RowOrder = 4, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=350, AppMetaCode = "ITEM", MetaType = "PANEL", MetaCode = "ITMPNL_B", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 2 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=360, AppMetaCode = "ITEM", MetaType = "COMBOBOX", MetaCode = "CB_CATEGORY", DataMetaCode = "CATCODE", Title = "Category", ParentMetaCode = "ITMPNL_B", RowOrder = 1, ColumnOrder = 1, Domain = "VALUEDOMAIN.ITEMCATEGORY" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=370, AppMetaCode = "ITEM", MetaType = "NUMBOX", MetaCode = "NUMBOX_PURCHPRICE", DataMetaCode = "PURCHASEPRICE", Title = "Purchase Price", ParentMetaCode = "ITMPNL_B", RowOrder = 2, ColumnOrder = 1 });

            //LISTVIEW
            uiitem.InsertOne(new UserInterfaceItem() { Id=380, AppMetaCode = "ITEM", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Item List", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=390, AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=400, AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ITEMID", DataMetaCode = "ITEMID", Title = "Item ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=410, AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ITEMNAME", DataMetaCode = "NAME", Title = "Item Name", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });
            #endregion

            #region sales order
            //APPLICATION SALESORDER
            //--------------------
            //DATABASE - MAINTABLE
            dbitem.InsertOne(new DatabaseItem() { Id=420, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ORDERNO", DbName = "OrderNo", ParentMetaCode = "ROOT", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=430, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ORDERDATE", DbName = "OrderDate", ParentMetaCode = "ROOT", DataType = "DATETIME" });
            dbitem.InsertOne(new DatabaseItem() { Id=440, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "CUSTID", DbName = "CustomerId", ParentMetaCode = "ROOT", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=450, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "CUSTNAME", DbName = "CustomerName", ParentMetaCode = "ROOT", DataType = "STRING" });
            //DATABASE - SUBTABLE (SALESLINES)
            dbitem.InsertOne(new DatabaseItem() { Id=460, AppMetaCode = "SALESORDER", MetaType = "DATATABLE", MetaCode = "DTORDLINE", DbName = "SalesLine", ParentMetaCode = "ROOT", DataType = "" });
            dbitem.InsertOne(new DatabaseItem() { Id=470, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ITEMID", DbName = "ItemNo", ParentMetaCode = "DTORDLINE", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=480, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ITEMNAME", DbName = "ItemName", ParentMetaCode = "DTORDLINE", DataType = "STRING" });
            dbitem.InsertOne(new DatabaseItem() { Id=490, AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "QTY", DbName = "Qty", ParentMetaCode = "DTORDLINE", DataType = "INTEGER" });

            //LISTVIEW
            uiitem.InsertOne(new UserInterfaceItem() { Id=500, AppMetaCode = "SALESORDER", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Sales Orders", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0, Properties = "" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=510, AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=520, AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LF_ORDERID", DataMetaCode = "ORDERNO", Title = "Order No", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=530, AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LF_CUSTNAME", DataMetaCode = "CUSTNAME", Title = "Customer", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });

            //UI
            uiitem.InsertOne(new UserInterfaceItem() { Id=540, AppMetaCode = "SALESORDER", MetaType = "SECTION", MetaCode = "SECT_HDR", DataMetaCode = "", Title = "Sales Header", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=550, AppMetaCode = "SALESORDER", MetaType = "PANEL", MetaCode = "PANEL1", DataMetaCode = "", Title = "", ParentMetaCode = "SECT_HDR", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=560, AppMetaCode = "SALESORDER", MetaType = "TEXTBOX", MetaCode = "TB_ORDERNO", DataMetaCode = "ORDERNO", Title = "Order No", ParentMetaCode = "PANEL1", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=570, AppMetaCode = "SALESORDER", MetaType = "DATEPICKER", MetaCode = "DP_ORDERDATE", DataMetaCode = "ORDERDATE", Title = "Order Date", ParentMetaCode = "PANEL1", RowOrder = 2, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=580, AppMetaCode = "SALESORDER", MetaType = "LOOKUP", MetaCode = "LOOKUP_CUSTOMER", DataMetaCode = "CUSTID", ViewMetaCode = "VCUSTID", DataMetaCode2 = "CUSTNAME", ViewMetaCode2 = "VCUSTNAME", Title = "Customer", ParentMetaCode = "PANEL1", RowOrder = 3, ColumnOrder = 1, Domain = "DATAVIEW.CUSTOMERVIEW" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=590, AppMetaCode = "SALESORDER", MetaType = "SECTION", MetaCode = "SECT_LINES", DataMetaCode = "", Title = "Sales Lines", ParentMetaCode = "ROOT", RowOrder = 2, ColumnOrder = 1, Properties = "COLLAPSIBLE=TRUE#STARTEXPANDED=TRUE" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=600, AppMetaCode = "SALESORDER", MetaType = "PANEL", MetaCode = "PANEL2", DataMetaCode = "", Title = "", ParentMetaCode = "SECT_LINES", RowOrder = 1, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=610, AppMetaCode = "SALESORDER", MetaType = "EDITGRID", MetaCode = "LINETABLE", DataMetaCode = "DTORDLINE", Title = "Sales Lines", ParentMetaCode = "PANEL2", RowOrder = 4, ColumnOrder = 1 });
            uiitem.InsertOne(new UserInterfaceItem() { Id=620, AppMetaCode = "SALESORDER", MetaType = "EDITGRID_LOOKUP", MetaCode = "LINE_ITEMID", DataMetaCode = "ITEMID", ViewMetaCode = "ITEMID", Title = "Item", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 1, Domain = "DATAVIEW.ITEMLOOKUP" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=630, AppMetaCode = "SALESORDER", MetaType = "EDITGRID_TEXTBOX", MetaCode = "LINE_ITEMNAME", DataMetaCode = "ITEMNAME", ViewMetaCode = "ITEMNAME", Title = "Item Name", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 1, Domain = "DATAVIEW.ITEMLOOKUP" });
            uiitem.InsertOne(new UserInterfaceItem() { Id=640, AppMetaCode = "SALESORDER", MetaType = "EDITGRID_NUMBOX", MetaCode = "LINE_QTY", DataMetaCode = "QTY", Title = "Qty", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 2 });
            #endregion

            #region dataviews
            //DATAVIEWS
            //---------
            //CUSTOMER
            dataview.InsertOne(new DataViewItem() { Id=650, MetaType = "DATAVIEW", MetaCode = "CUSTOMERVIEW", ParentMetaCode = "ROOT", SQLQuery = "select CustomerId, CustomerName from Customer order by CustomerId asc", Title = "Customers", SQLQueryFieldName = "" });
            dataview.InsertOne(new DataViewItem() { Id=660, MetaType = "DATAVIEWKEYCOLUMN", MetaCode = "VCUSTID", ParentMetaCode = "CUSTOMERVIEW", SQLQuery = "", Title = "Id", SQLQueryFieldName = "CustomerId" });
            dataview.InsertOne(new DataViewItem() { Id=670, MetaType = "DATAVIEWCOLUMN", MetaCode = "VCUSTNAME", ParentMetaCode = "CUSTOMERVIEW", SQLQuery = "", Title = "Name", SQLQueryFieldName = "CustomerName" });

            //ITEM
            dataview.InsertOne(new DataViewItem() { Id=680, MetaType = "DATAVIEW", MetaCode = "ITEMLOOKUP", ParentMetaCode = "ROOT", SQLQuery = "select ItemId, ItemName  from Item order by ItemId asc", Title = "Items", SQLQueryFieldName = "" });
            dataview.InsertOne(new DataViewItem() { Id=690, MetaType = "DATAVIEWKEYCOLUMN", MetaCode = "ITEMID", ParentMetaCode = "ITEMLOOKUP", SQLQuery = "", Title = "Id", SQLQueryFieldName = "ItemId" });
            dataview.InsertOne(new DataViewItem() { Id=700, MetaType = "DATAVIEWCOLUMN", MetaCode = "ITEMNAME", ParentMetaCode = "ITEMLOOKUP", SQLQuery = "", Title = "Name", SQLQueryFieldName = "ItemName" });
            #endregion

        }

        public static void SeedSql(IServiceProvider provider)
        {
            var Settings = provider.GetRequiredService<IOptions<SystemSettings>>();
            var Connections = provider.GetRequiredService<IOptions<ConnectionStrings>>();
            var DataRepository = new IntwentySqlDbClient((DBMS)Settings.Value.IntwentyDBMS, Connections.Value.IntwentyConnection);


            DataRepository.CreateTable<ApplicationItem>(true);
            DataRepository.CreateTable<DatabaseItem>(true);
            DataRepository.CreateTable<DataViewItem>(true);
            DataRepository.CreateTable<EventLog>(true);
            DataRepository.CreateTable<InformationStatus>(true);
            DataRepository.CreateTable<MenuItem>(true);
            DataRepository.CreateTable<SystemID>(true);
            DataRepository.CreateTable<UserInterfaceItem>(true);
            DataRepository.CreateTable<ValueDomainItem>(true);

            if (Settings.Value.ReCreateModelOnStartUp)
            {
                DataRepository.DeleteRange(DataRepository.Get<ApplicationItem>());
                DataRepository.DeleteRange(DataRepository.Get<DatabaseItem>());
                DataRepository.DeleteRange(DataRepository.Get<DataViewItem>());
                DataRepository.DeleteRange(DataRepository.Get<MenuItem>());
                DataRepository.DeleteRange(DataRepository.Get<UserInterfaceItem>());
                DataRepository.DeleteRange(DataRepository.Get<ValueDomainItem>());
            }


            //APPLICATIONS
            DataRepository.Insert(new ApplicationItem() { Id = 10, Description = "An app for managing customers", MetaCode = "CUSTOMER", Title = "Customer", DbName = "Customer", IsHierarchicalApplication = false, UseVersioning = false });
            DataRepository.Insert(new ApplicationItem() { Id = 20, Description = "An app for managing items", MetaCode = "ITEM", Title = "Item", DbName = "Item", IsHierarchicalApplication = false, UseVersioning = false });
            DataRepository.Insert(new ApplicationItem() { Id = 30, Description = "An app for managing sales orders", MetaCode = "SALESORDER", Title = "Sales Order", DbName = "SalesHeader", IsHierarchicalApplication = false, UseVersioning = false });

            //MENU
            DataRepository.Insert(new MenuItem() { AppMetaCode = "", MetaType = "MAINMENU", MetaCode = "SYSMENU", ParentMetaCode = "ROOT", Title = "Menu", OrderNo = 1, Action = "", Controller = "" });
            DataRepository.Insert(new MenuItem() { AppMetaCode = "CUSTOMER", MetaType = "MENUITEM", MetaCode = "M_CUST", ParentMetaCode = "SYSMENU", Title = "Customer", OrderNo = 1, Action = "", Controller = "" });
            DataRepository.Insert(new MenuItem() { AppMetaCode = "ITEM", MetaType = "MENUITEM", MetaCode = "M_ITEM", ParentMetaCode = "SYSMENU", Title = "Item", OrderNo = 10, Action = "", Controller = "" });
            DataRepository.Insert(new MenuItem() { AppMetaCode = "SALESORDER", MetaType = "MENUITEM", MetaCode = "M_SORD", ParentMetaCode = "SYSMENU", Title = "Sales Order", OrderNo = 20, Action = "", Controller = "" });

            //VALUEDOMAIN (USED IN COMBOBOXES ETC)
            DataRepository.Insert(new ValueDomainItem() { DomainName = "ITEMCATEGORY", Code = "A1", Value = "Primary" });
            DataRepository.Insert(new ValueDomainItem() { DomainName = "ITEMCATEGORY", Code = "A2", Value = "Secondary" });


            #region customer
            //APPLICATION CUSTOMER
            //--------------------
            //DATABASE - MAINTABLE
            DataRepository.Insert(new DatabaseItem() { AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERID", DbName = "CustomerId", ParentMetaCode = "ROOT", DataType = "STRING", Mandatory = true });
            DataRepository.Insert(new DatabaseItem() { AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERNAME", DbName = "CustomerName", ParentMetaCode = "ROOT", DataType = "STRING" });
            DataRepository.Insert(new DatabaseItem() { AppMetaCode = "CUSTOMER", MetaType = "DATACOLUMN", MetaCode = "CUSTOMERINFO", DbName = "CustomerInfo", ParentMetaCode = "ROOT", DataType = "TEXT" });

            //UI
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "SECTION", MetaCode = "MAINSECTION", DataMetaCode = "", Title = "Sales Header", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "PANEL", MetaCode = "CUSTPNL1", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "TEXTBOX", MetaCode = "TB_CUSTID", DataMetaCode = "CUSTOMERID", Title = "Customer ID", ParentMetaCode = "CUSTPNL1", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "TEXTBOX", MetaCode = "TB_CUSTNAME", DataMetaCode = "CUSTOMERNAME", Title = "Customer Name", ParentMetaCode = "CUSTPNL1", RowOrder = 2, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "PANEL", MetaCode = "CUSTPNL2", DataMetaCode = "", Title = "Information", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 2 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "TEXTAREA", MetaCode = "TA_CUSTINFO", DataMetaCode = "CUSTOMERINFO", Title = "Customer Notes", ParentMetaCode = "CUSTPNL2", RowOrder = 3, ColumnOrder = 1 });

            //LISTVIEW
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Customer List", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_CUSTID", DataMetaCode = "CUSTOMERID", Title = "Customer ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "CUSTOMER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_CUSTNAME", DataMetaCode = "CUSTOMERNAME", Title = "Customer Name", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });
            #endregion

            #region item
            //APPLICATION ITEM
            //---------------------
            //DATABASE - MAINTABLE
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "ITEMID", DbName = "ItemId", ParentMetaCode = "ROOT", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "NAME", DbName = "ItemName", ParentMetaCode = "ROOT", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "CATCODE", DbName = "ItemCategoryCode", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "VALUEDOMAIN.ITEMCATEGORY.CODE" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "CATVALUE", DbName = "ItemCategoryValue", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "VALUEDOMAIN.ITEMCATEGORY.VALUE" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "MODIFIED", DbName = "Modified", ParentMetaCode = "ROOT", DataType = "DATETIME" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "ACTIVE", DbName = "Active", ParentMetaCode = "ROOT", DataType = "BOOLEAN" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "PURCHASEPRICE", DbName = "PurchasePrice", ParentMetaCode = "ROOT", DataType = "2DECIMAL" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "VENDORCODE", DbName = "VendorCode", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "APP.VENDOR.VENDORID" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "ITEM", MetaType = "DATACOLUMN", MetaCode = "VENDORTXT", DbName = "VendorName", ParentMetaCode = "ROOT", DataType = "STRING", Domain = "APP.VENDOR.VENDORNAME" });

            //UI
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "SECTION", MetaCode = "MAINSECTION", DataMetaCode = "", Title = "Basics", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "PANEL", MetaCode = "ITMPNL_A", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "TEXTBOX", MetaCode = "TB_ITID", DataMetaCode = "ITEMID", Title = "Item ID", ParentMetaCode = "ITMPNL_A", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "TEXTBOX", MetaCode = "TB_ITNAME", DataMetaCode = "NAME", Title = "Item Name", ParentMetaCode = "ITMPNL_A", RowOrder = 2, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "DATEPICKER", MetaCode = "DP_MOD", DataMetaCode = "MODIFIED", Title = "Modified Date", ParentMetaCode = "ITMPNL_A", RowOrder = 3, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "CHECKBOX", MetaCode = "CB_ACTIVE", DataMetaCode = "ACTIVE", Title = "Is Active", ParentMetaCode = "ITMPNL_A", RowOrder = 4, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "PANEL", MetaCode = "ITMPNL_B", DataMetaCode = "", Title = "Basics", ParentMetaCode = "MAINSECTION", RowOrder = 1, ColumnOrder = 2 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "COMBOBOX", MetaCode = "CB_CATEGORY", DataMetaCode = "CATCODE", Title = "Category", ParentMetaCode = "ITMPNL_B", RowOrder = 1, ColumnOrder = 1, Domain = "VALUEDOMAIN.ITEMCATEGORY" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "NUMBOX", MetaCode = "NUMBOX_PURCHPRICE", DataMetaCode = "PURCHASEPRICE", Title = "Purchase Price", ParentMetaCode = "ITMPNL_B", RowOrder = 2, ColumnOrder = 1 });

            //LISTVIEW
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Item List", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ITEMID", DataMetaCode = "ITEMID", Title = "Item ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "ITEM", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ITEMNAME", DataMetaCode = "NAME", Title = "Item Name", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });
            #endregion

            #region sales order
            //APPLICATION SALESORDER
            //--------------------
            //DATABASE - MAINTABLE
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ORDERNO", DbName = "OrderNo", ParentMetaCode = "ROOT", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ORDERDATE", DbName = "OrderDate", ParentMetaCode = "ROOT", DataType = "DATETIME" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "CUSTID", DbName = "CustomerId", ParentMetaCode = "ROOT", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "CUSTNAME", DbName = "CustomerName", ParentMetaCode = "ROOT", DataType = "STRING" });
            //DATABASE - SUBTABLE (SALESLINES)
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATATABLE", MetaCode = "DTORDLINE", DbName = "SalesLine", ParentMetaCode = "ROOT", DataType = "" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ITEMID", DbName = "ItemNo", ParentMetaCode = "DTORDLINE", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "ITEMNAME", DbName = "ItemName", ParentMetaCode = "DTORDLINE", DataType = "STRING" });
             DataRepository.Insert(new DatabaseItem() { AppMetaCode = "SALESORDER", MetaType = "DATACOLUMN", MetaCode = "QTY", DbName = "Qty", ParentMetaCode = "DTORDLINE", DataType = "INTEGER" });

            //LISTVIEW
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "LISTVIEW", MetaCode = "MAIN_LISTVIEW", DataMetaCode = "", Title = "Sales Orders", ParentMetaCode = "ROOT", RowOrder = 0, ColumnOrder = 0, Properties = "" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LV_ID", DataMetaCode = "ID", Title = "ID", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LF_ORDERID", DataMetaCode = "ORDERNO", Title = "Order No", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 2 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "LISTVIEWFIELD", MetaCode = "LF_CUSTNAME", DataMetaCode = "CUSTNAME", Title = "Customer", ParentMetaCode = "MAIN_LISTVIEW", RowOrder = 1, ColumnOrder = 3 });

            //UI
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "SECTION", MetaCode = "SECT_HDR", DataMetaCode = "", Title = "Sales Header", ParentMetaCode = "ROOT", RowOrder = 1, ColumnOrder = 1, Properties = "COLLAPSIBLE=FALSE#STARTEXPANDED=FALSE" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "PANEL", MetaCode = "PANEL1", DataMetaCode = "", Title = "", ParentMetaCode = "SECT_HDR", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "TEXTBOX", MetaCode = "TB_ORDERNO", DataMetaCode = "ORDERNO", Title = "Order No", ParentMetaCode = "PANEL1", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "DATEPICKER", MetaCode = "DP_ORDERDATE", DataMetaCode = "ORDERDATE", Title = "Order Date", ParentMetaCode = "PANEL1", RowOrder = 2, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "LOOKUP", MetaCode = "LOOKUP_CUSTOMER", DataMetaCode = "CUSTID", ViewMetaCode = "VCUSTID", DataMetaCode2 = "CUSTNAME", ViewMetaCode2 = "VCUSTNAME", Title = "Customer", ParentMetaCode = "PANEL1", RowOrder = 3, ColumnOrder = 1, Domain = "DATAVIEW.CUSTOMERVIEW" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "SECTION", MetaCode = "SECT_LINES", DataMetaCode = "", Title = "Sales Lines", ParentMetaCode = "ROOT", RowOrder = 2, ColumnOrder = 1, Properties = "COLLAPSIBLE=TRUE#STARTEXPANDED=TRUE" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "PANEL", MetaCode = "PANEL2", DataMetaCode = "", Title = "", ParentMetaCode = "SECT_LINES", RowOrder = 1, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "EDITGRID", MetaCode = "LINETABLE", DataMetaCode = "DTORDLINE", Title = "Sales Lines", ParentMetaCode = "PANEL2", RowOrder = 4, ColumnOrder = 1 });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "EDITGRID_LOOKUP", MetaCode = "LINE_ITEMID", DataMetaCode = "ITEMID", ViewMetaCode = "ITEMID", Title = "Item", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 1, Domain = "DATAVIEW.ITEMLOOKUP" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "EDITGRID_TEXTBOX", MetaCode = "LINE_ITEMNAME", DataMetaCode = "ITEMNAME", ViewMetaCode = "ITEMNAME", Title = "Item Name", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 1, Domain = "DATAVIEW.ITEMLOOKUP" });
             DataRepository.Insert(new UserInterfaceItem() { AppMetaCode = "SALESORDER", MetaType = "EDITGRID_NUMBOX", MetaCode = "LINE_QTY", DataMetaCode = "QTY", Title = "Qty", ParentMetaCode = "LINETABLE", RowOrder = 1, ColumnOrder = 2 });
            #endregion

            #region dataviews
            //DATAVIEWS
            //---------
            //CUSTOMER
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEW", MetaCode = "CUSTOMERVIEW", ParentMetaCode = "ROOT", SQLQuery = "select CustomerId, CustomerName from Customer order by CustomerId asc", Title = "Customers", SQLQueryFieldName = "" });
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEWKEYCOLUMN", MetaCode = "VCUSTID", ParentMetaCode = "CUSTOMERVIEW", SQLQuery = "", Title = "Id", SQLQueryFieldName = "CustomerId" });
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEWCOLUMN", MetaCode = "VCUSTNAME", ParentMetaCode = "CUSTOMERVIEW", SQLQuery = "", Title = "Name", SQLQueryFieldName = "CustomerName" });

            //ITEM
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEW", MetaCode = "ITEMLOOKUP", ParentMetaCode = "ROOT", SQLQuery = "select ItemId, ItemName  from Item order by ItemId asc", Title = "Items", SQLQueryFieldName = "" });
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEWKEYCOLUMN", MetaCode = "ITEMID", ParentMetaCode = "ITEMLOOKUP", SQLQuery = "", Title = "Id", SQLQueryFieldName = "ItemId" });
             DataRepository.Insert(new DataViewItem() { MetaType = "DATAVIEWCOLUMN", MetaCode = "ITEMNAME", ParentMetaCode = "ITEMLOOKUP", SQLQuery = "", Title = "Name", SQLQueryFieldName = "ItemName" });
            #endregion

        
        }
    }
}
