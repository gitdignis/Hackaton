using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using static myDashboard.Controllers.DeserializeFilter;

namespace myDashboard.Controllers
{
    public static class Column
    {
        public static bool IsValid(string dataIndx)
        {
            return Regex.IsMatch(dataIndx, "^[a-z,A-Z,0-9,_]*$");
        }
    }

    public static class DeserializeOrderBy
    {
        public struct Sort
        {
            public string dataIndx;
            public string dir;
        }
        
        public static string MakeFilter(string field, string direction = "asc")
        {
            List<Sort> f = new List<Sort> {
                new Sort {
                    dataIndx = field,
                    dir = direction
                }
            };

            JavaScriptSerializer js = new JavaScriptSerializer();

            return js.Serialize(f);
        }

        public static IQueryable<T> Deserialize<T>(
            this IQueryable<T> source, 
            string pq_sort, 
            ref int sortTotal,
            string sortClauseDefault = null, 
            string sortClausePre = null, 
            string sortClausePost = null
        )
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            List<Sort> sorters = js.Deserialize<List<Sort>>(!string.IsNullOrEmpty(pq_sort) ? pq_sort : "");
            List<string> columns = new List<string>();

            if (!string.IsNullOrEmpty(sortClausePre))
            {
                columns.Add(sortClausePre);
            }

            if (sorters != null)
            {
                foreach (Sort sorter in sorters)
                {
                    string dataIndx = sorter.dataIndx;
                    string dir = sorter.dir == "up" ? "asc" : "desc";

                    if (Column.IsValid(dataIndx))
                    {
                        PropertyInfo convertProperty = typeof(T).GetProperty(dataIndx);
                        if (convertProperty != null)
                        {
                            columns.Add("@" + dataIndx + " " + dir);
                            sortTotal++;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(sortClausePost))
            {
                columns.Add(sortClausePost);
            }

            if (columns.Count == 0 && !string.IsNullOrEmpty(sortClauseDefault))
            {
                columns.Add(sortClauseDefault);
            }

            if (columns.Count > 0)
            {
                string sortby = string.Join(", ", columns);
                return source.OrderBy(sortby);
            }
            return source;
        }
    }

    public static class DeserializeFilter
    {
        public struct Filter
        {
            public string dataIndx;
            public string condition;
            public string value;
        }

        //map to json object posted by client
        public struct Filters
        {
            public string mode;
            public List<Filter> data;
        }

        public static string MakeFilter(string field, string value, string condition = "equal")
        {
            Filters f = new Filters
            {
                mode = "AND",
                data = new List<Filter>
                    {
                        new Filter
                        {
                            condition = condition,
                            dataIndx = field,
                            value = value
                        }
                    }
            };

            JavaScriptSerializer js = new JavaScriptSerializer();

            return js.Serialize(f);
        }

        public static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static bool IsNullableType(MemberExpression member)
        {
            Type t = DeserializeFilter.GetType(member);
            return Nullable.GetUnderlyingType(t) != null;
        }

        public static Type GetType(MemberExpression member)
        {
            return member.Member is MethodInfo ? ((MethodInfo)member.Member).ReturnType : ((PropertyInfo)member.Member).PropertyType;
        }
        public static Type GetNullableType(MemberExpression member)
        {
            Type t = DeserializeFilter.GetType(member);
            t = Nullable.GetUnderlyingType(t) ?? t;
            t = (t.IsValueType) ? typeof(Nullable<>).MakeGenericType(t) : t;

            return t;
        }


        public static ConstantExpression GetConstant(MemberExpression member, string value)
        {
            Type t = GetType(member);
            TypeConverter converter = TypeDescriptor.GetConverter(t);

            return Expression.Constant(converter.ConvertFromInvariantString(value), t);
        }
        public static ConstantExpression GetNullConstant(MemberExpression member)
        {
            Type t = GetNullableType(member);

            //TypeConverter converter = TypeDescriptor.GetConverter(t);

            return Expression.Constant(null, t);
        }

        public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> property)
        {
            MemberExpression memberExpression = (MemberExpression)property.Body;
            return memberExpression.Member.Name;
        }

        public static readonly MethodInfo containsMethod = typeof(string).GetMethod("Contains");
        public static readonly MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
        public static readonly MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });

        public static readonly MethodInfo truncateDateTimeMethod = typeof(DbFunctions).GetMethod("TruncateTime", new Type[] { typeof(DateTime?) });

        public static BinaryExpression Equal(MemberExpression member, string value)
        {
            return Expression.Equal(member, GetConstant(member, value));
        }
        public static BinaryExpression Equal<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.Equal(member, GetConstant(member, value));
        }


        public static MethodCallExpression Contains(MemberExpression member, string value)
        {
            return Expression.Call(member, containsMethod, GetConstant(member, value));
        }
        public static MethodCallExpression Contains<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.Call(member, containsMethod, GetConstant(member, value));
        }


        public static MethodCallExpression StartsWith(MemberExpression member, string value)
        {
            return Expression.Call(member, startsWithMethod, GetConstant(member, value));
        }
        public static MethodCallExpression StartsWith<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.Call(member, startsWithMethod, GetConstant(member, value));
        }


        public static MethodCallExpression EndsWith(MemberExpression member, string value)
        {
            return Expression.Call(member, endsWithMethod, GetConstant(member, value));
        }
        public static MethodCallExpression EndsWith<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.Call(member, endsWithMethod, GetConstant(member, value));
        }


        public static BinaryExpression GreaterThan(MemberExpression member, string value)
        {
            return Expression.GreaterThan(member, GetConstant(member, value));
        }
        public static BinaryExpression GreaterThan<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.GreaterThan(member, GetConstant(member, value));
        }


        public static BinaryExpression LessThan(MemberExpression member, string value)
        {
            return Expression.LessThan(member, GetConstant(member, value));
        }
        public static BinaryExpression LessThan<T, TProperty>(ParameterExpression param, Expression<Func<T, TProperty>> property, string value)
        {
            MemberExpression member = Expression.Property(param, GetPropertyName(property));
            return Expression.LessThan(member, GetConstant(member, value));
        }


        public delegate Expression callbackExpression<T>(ParameterExpression param, string value);

        private static Expression GetExpression<T>(ParameterExpression param, Filter filter, callbackExpression<T> callback)
        {
            // handle custom expressions
            if (filter.condition == "_remove_")
            {
                return null;
            }
            if (filter.condition == "pattern")
            {
                if (callback == null) {
                    return null;
                }
                return callback(param, filter.value);
            }

            MemberExpression member = Expression.Property(param, filter.dataIndx);

            //if ((filter.condition == "empty" || filter.condition == "notempty") && !IsNullableType(member))
            //{
            //    return null;
            //}

            ConstantExpression constant = (filter.condition == "empty" || filter.condition == "notempty") ? 
                GetNullConstant(member) : 
                GetConstant(member, filter.value);

            if (GetType(member) == typeof(DateTime) || GetType(member) == typeof(DateTime?))
            {
                MethodCallExpression truncatedConstant = Expression.Call(null, truncateDateTimeMethod, Expression.Convert(constant, typeof(DateTime?)));
                MethodCallExpression truncatedMember = Expression.Call(null, truncateDateTimeMethod, Expression.Convert(member, typeof(DateTime?)));
                switch (filter.condition)
                {
                    case "equal":
                        return Expression.Equal(truncatedMember, truncatedConstant);

                    case "notequal":
                        return Expression.Not(Expression.Equal(truncatedMember, truncatedConstant));

                    case "great":
                        return Expression.GreaterThan(truncatedMember, truncatedConstant);

                    //case Op.GreaterThanOrEqual:
                    //    return Expression.GreaterThanOrEqual(member, constant);

                    case "less":
                        return Expression.LessThan(truncatedMember, truncatedConstant);

                    //case Op.LessThanOrEqual:
                    //    return Expression.LessThanOrEqual(member, constant);

                }
            }

            switch (filter.condition)
            {
                case "equal":
                    return Expression.Equal(member, constant);

                case "notequal":
                    return Expression.Not(Expression.Equal(member, constant));

                case "great":

                    return Expression.GreaterThan(member, constant);

                //case Op.GreaterThanOrEqual:
                //    return Expression.GreaterThanOrEqual(member, constant);

                case "less":
                    return Expression.LessThan(member, constant);

                //case Op.LessThanOrEqual:
                //    return Expression.LessThanOrEqual(member, constant);

                case "contain":
                    return Expression.Call(member, containsMethod, constant);

                case "notcontain":
                    return Expression.Not(Expression.Call(member, containsMethod, constant));

                case "begin":
                    return Expression.Call(member, startsWithMethod, constant);

                case "end":
                    return Expression.Call(member, endsWithMethod, constant);

                case "empty":
                    //return Expression.Equal(member, constant);
                    return Expression.Equal(member, constant);

                case "notempty":
                    return Expression.Not(Expression.Equal(member, constant));

            }

            return null;
        }

        public static Expression<Func<T, bool>> GetExpression<T>(string mode, IList<Filter> filters, callbackExpression<T> callback, ref int filtersTotal)
        {
            //filtersTotal = 0;

            if (filters.Count == 0)
            {
                return null;
            }

            List<Expression> expressions = new List<Expression>();
            ParameterExpression param = Expression.Parameter(typeof(T), "t");

            foreach (var filter in filters)
            {
                Expression expression = GetExpression<T>(param, filter, callback);
                if (expression != null)
                {
                    expressions.Add(expression);
                }
            }

            if (expressions.Count == 0)
            {
                return null;
            }

            Expression expressionComposed = null;

            for (int i = 0; i < expressions.Count; i++)
            {
                if (i == 0)
                {
                    expressionComposed = expressions[i];
                }
                else
                {
                    if (mode == "AND") {
                        expressionComposed = Expression.AndAlso(expressionComposed, expressions[i]);
                    } else {
                        expressionComposed = Expression.OrElse(expressionComposed, expressions[i]);
                    }
                }
            }

            filtersTotal += expressions.Count;

            return Expression.Lambda<Func<T, bool>>(expressionComposed, param);
        }

        private static BinaryExpression GetExpression<T>(ParameterExpression param, Filter filter1, Filter filter2, callbackExpression<T> callback)
        {
            Expression bin1 = GetExpression<T>(param, filter1, callback);
            Expression bin2 = GetExpression<T>(param, filter2, callback);

            return Expression.AndAlso(bin1, bin2);
        }


        public static IQueryable<T> Deserialize<T>(this IQueryable<T> source, String pq_filter, callbackExpression<T> callback, ref int filtersTotal)
        {
            if (string.IsNullOrEmpty(pq_filter))
            {
                return source;
            }

            JavaScriptSerializer js = new JavaScriptSerializer();

            Filters filterObj = js.Deserialize<Filters>(pq_filter);
            String mode = filterObj.mode;
            List<Filter> filters = filterObj.data;

            var expressions = GetExpression<T>(mode, filters, callback, ref filtersTotal);

            if (expressions == null)
            {
                return source;
            }

            return source.Where(expressions);
        }
    }

    public class _GridController : _Controller
    {
        [DefaultValue(null)]
        protected string pq_sort { get; set; }

        [DefaultValue(null)]
        protected string pq_filter { get; set; }

        [DefaultValue(1)]
        protected int pq_curpage { get; set; }

        [DefaultValue(10)]
        protected int pq_rpp { get; set; }

        protected int totalRecords = 0;             // physical rows

        protected int totalLogicalRecords = 0;     // logical physical rows

        [DefaultValue(1)]
        protected int pageSize { get { return pq_rpp; } }

        [DefaultValue(1)]
        protected int skip { get { return _skip(); } }
        private int _skip() {
            int s = pq_rpp * (pq_curpage - 1);
            if (s >= totalRecords)
            {
                pq_curpage = (int)Math.Ceiling(((double)totalRecords) / pq_rpp) - 1;
                if (pq_curpage < 1)
                {
                    pq_curpage = 1;
                }
                return pq_rpp * (pq_curpage - 1);
            }
            return s;
        }

        protected string TranslateGridParameters(string grid_filter, Func<DeserializeFilter.Filter, DeserializeFilter.Filter> callback)
        {
            if (string.IsNullOrEmpty(grid_filter))
            {
                return grid_filter;
            }

            JavaScriptSerializer js = new JavaScriptSerializer();

            Filters filterObj = js.Deserialize<Filters>(grid_filter);
            List<DeserializeFilter.Filter> filters = filterObj.data;

            for (int i = 0; i < filters.Count(); i++)
            {
                DeserializeFilter.Filter filter = filters[i];
                filters[i] = callback(filter);
            }

            return js.Serialize(filterObj);
        }

        protected void GetGridParameters(Func<DeserializeFilter.Filter, DeserializeFilter.Filter> callbackTranslate = null)
        {
            pq_sort = GetFormValue("pq_sort");
            pq_filter = GetFormValue("pq_filter");

            if (callbackTranslate != null)
            {
                pq_filter = TranslateGridParameters(pq_filter, callbackTranslate);
            }

            if (GetFormValue("pq_type") == "remote")
            {
                Int32.TryParse(GetFormValue("pq_curpage"), out int value);
                pq_curpage = value;
                if (pq_curpage < 1)
                {
                    pq_curpage = 1;
                }

                if (Int32.TryParse(GetFormValue("pq_rpp"), out value))
                {
                    pq_rpp = value;
                }
                else
                {
                    pq_rpp = 10;
                }
            } else
            {
                pq_curpage = 1;
                pq_rpp = 10000;
            }

            totalRecords = 0;
        }
        public JsonResult JsonGrid<T>(List<T> view)
        {
            return JsonObject(new {
                curPage = pq_curpage,
                totalRecords,
                totalLogicalRecords,
                data = view
            });
        }

        public JsonResult JsonObj<T>(T view)
        {
            return JsonObject(new
            {
                data = view
            });
        }
    }
}