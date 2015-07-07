# TeleMongo
This is a small extension that I started writing to support Mongo from the Telerik DataSourceRequest object.

The extension method DataSourceRequest.GetPagedData<T>() will translate the Telerik DataSourceRequest object from a DataGrid into a Mongo FluentAPI query and then execute it.

# TeleMongo
This is a small extension that I started writing to support Mongo from the Telerik DataSourceRequest object.

The extension method DataSourceRequest.GetPagedData<T>() will translate the Telerik DataSourceRequest object from a DataGrid into a Mongo FluentAPI query and then execute it.

    //Sample Code
    public async Task<ActionResult> ReadSomeData([DataSourceRequest]DataSourceRequest request)
    {
    	try
    	{
    		var collection = MongoDB.GetMongoCollection<Model>("CollectionName");
    
    		return Json(await request.GetPagedData<Model>(collection), 
                JsonRequestBehavior.AllowGet);
    	}
    	catch (Exception ex)
    	{
    		ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);

    		return Json(new List<Model> { new Model() }.ToDataSourceResult(request, 
    		    ModelState), JsonRequestBehavior.AllowGet);
    	}
    }
