using Kendo.Mvc;
using Kendo.Mvc.UI;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace QuintonV
{
    public static class MongoHelper
    {
        internal static async Task<DataSourceResult> GetPagedData<T>(this DataSourceRequest request, IMongoCollection<T> collection)
        {
            var sortBuilder = Builders<T>.Sort;

            SortDefinition<T> sort = GetSortDefinition<T>(request, sortBuilder);

            int skip = request.Page - 1;

            if (request.Page > 1)
                skip = skip * request.PageSize;

            FilterDefinition<T> filter = GetFilters<T>(request);

            List<T> data = null;

            long total = 0;

            if (filter == null)
            {
                data = await collection.Find<T>(new BsonDocument()).Sort(sort).Skip(skip).Limit(request.PageSize).ToListAsync();
                total = await collection.Find<T>(new BsonDocument()).CountAsync();
            }
            else
            {
                data = await collection.Find<T>(filter).Sort(sort).Skip(skip).Limit(request.PageSize).ToListAsync();
                total = await collection.Find<T>(filter).CountAsync();
            }

            return new DataSourceResult()
            {
                Data = data,
                Total = Convert.ToInt32(total)
            };
        }

        private static FilterDefinition<T> GetFilters<T>(DataSourceRequest request)
        {
            FilterDefinition<T> fd = null;

            foreach (var filter in request.Filters)
            {
                if (filter is FilterDescriptor)
                {
                    var f = filter as FilterDescriptor;
                    if (fd == null)
                    {
                        fd = GetFilter(fd, f);
                    }
                    else
                        fd = fd & GetFilter(fd, f);
                }
                if (filter is CompositeFilterDescriptor)
                {
                    var cf = filter as CompositeFilterDescriptor;

                    foreach (FilterDescriptor f in cf.FilterDescriptors)
                    {
                        if (fd == null)
                        {
                            fd = GetFilter(fd, f);

                        }
                        else
                            fd = fd & GetFilter(fd, f);
                    }
                }
            }
            return fd;
        }

        /// <summary>
        /// Translates the Telerik SortDescriptor to the Mongo SortDefinition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sortBuilder"></param>
        /// <returns></returns>
        private static SortDefinition<T> GetSortDefinition<T>(DataSourceRequest request, SortDefinitionBuilder<T> sortBuilder)
        {
            SortDefinition<T> sort = null;

            if (request.Sorts.Any())
            {
                // TODO: Support multiple sort statements
                foreach (SortDescriptor sortDescriptor in request.Sorts)
                {
                    if (sortDescriptor.SortDirection == ListSortDirection.Ascending)
                        sort = sortBuilder.Ascending(sortDescriptor.Member);
                    else
                        sort = sortBuilder.Descending(sortDescriptor.Member);
                    break;
                }
            }
            else
                // Might not need a default sort... I used it though for paging on the default mongo _Id
                // TODO: Enhance or paramterise this
                sort = Builders<T>.Sort.Ascending("_id");

            return sort;
        }

        /// <summary>
        /// Populates the Mongo FilterDefinition from the Telerik FilterDescriptor.
        /// </summary>
        /// <param name="filterDef">Mongo Query</param>
        /// <param name="filterDesc">Telerik Query</param>
        /// <returns></returns>
        private static FilterDefinition<T> GetFilter<T>(FilterDefinition<T> filterDef, FilterDescriptor filterDesc) //where T : class,new()
        {
            if (filterDesc.MemberType == null)
            {
                // Get the Type of the Value, for some reason this is always null...
                filterDesc.MemberType = FollowPropertyPath(typeof(T), filterDesc.Member);

                // The Value comes back as a string, cast the value to the Type it should be
                filterDesc.Value = Convert.ChangeType(filterDesc.Value, filterDesc.MemberType);
            }

            // TODO: Translate all the remaining Operators
            // Translate some of the Operators to Mongo
            switch (filterDesc.Operator)
            {
                case (FilterOperator.IsEqualTo):
                    {
                        filterDef = Builders<T>.Filter.Eq(filterDesc.Member, filterDesc.Value);

                        break;
                    }
                case (FilterOperator.IsNotEqualTo):
                    {
                        filterDef = Builders<T>.Filter.Ne(filterDesc.Member, filterDesc.Value);

                        break;
                    }
                case (FilterOperator.IsGreaterThanOrEqualTo):
                    {
                        filterDef = Builders<T>.Filter.Gte(filterDesc.Member, filterDesc.Value);

                        break;
                    }
                case (FilterOperator.IsLessThanOrEqualTo):
                    {
                        filterDef = Builders<T>.Filter.Lte(filterDesc.Member, filterDesc.Value);

                        break;
                    }
                case (FilterOperator.Contains):
                    {
                        filterDef = Builders<T>.Filter.Regex(filterDesc.Member, "/" + filterDesc.Value + "/");

                        break;
                    }
                case (FilterOperator.StartsWith):
                    {
                        filterDef = Builders<T>.Filter.Regex(filterDesc.Member, "/^" + filterDesc.Value + "/");

                        break;
                    }
                default:
                    throw new NotImplementedException(string.Format("The Telerik Operator {0} has not been implemented. Add it to the GetFilter() switch.",
                        filterDesc.Operator));
            }
            return filterDef;
        }

        /// <summary>
        /// Finds a Property inside a Class Type to get its Type.
        /// </summary>
        /// <param name="type">The Type to find the Property Path in</param>
        /// <param name="path">The full name of the Property</param>
        /// <returns></returns>
        private static Type FollowPropertyPath(Type type, string path)
        {
            foreach (string propertyName in path.Split('.'))
            {
                PropertyInfo property = type.GetProperty(propertyName);
                //   value = property.GetValue(value, null);
                type = property.PropertyType;
            }
            return type;
        }
    }
}
