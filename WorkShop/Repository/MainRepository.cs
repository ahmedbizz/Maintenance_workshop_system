
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WorkShop.Context;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Repository
{

    public class MainRepository<T> : IRepository<T> where T : class
    {
        public MainRepository(AppDbContext context) {
            this.Context = context;        
        }
        protected AppDbContext Context;

        //=========================Find=============================
        //Find By Id
        public T FindById(int? Id)
        {
            var item = Context.Set<T>().Find(Id);

            return item;
        }
        public T FindById(string? Id)
        {
            var item = Context.Set<T>().Find(Id);

            return item;
        }
        //Find By Idsas
        public ProductStock FindByKeys (int? productId, int storeId)
        {

            return Context.Set<ProductStock>().FirstOrDefault(p => p.productId == productId && p.storeId == storeId);

        }

        // For get all itemas in Table
        public IEnumerable<T> FindAll()
        {
           return Context.Set<T>().ToList();
        }
        public IEnumerable<T> FindAll(params string[] agers)
        {
            IQueryable<T> query = Context.Set<T>();

            if (agers.Length > 0)
            {
                foreach (var ager in agers)
                {
                    query = query.Include(ager);
                }
            }
            return query.ToList();
        }


        // To Search item in table

        public IQueryable<T> Search(params string[] includes)
        {
            IQueryable<T> query = Context.Set<T>();
            if (includes.Length > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }

        public IQueryable<T> SearchBycondition(Expression<Func<T, bool>> expression, params string[] includes)
        {
            IQueryable<T> query = Context.Set<T>();
            if (includes.Length > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query.Where(expression);
        }

        //=========================Insert=============================
        // To Add Item in Table 
        public void Insert(T entity)
        {

            Context.Set<T>().Add(entity);
            Context.SaveChanges();
        }
        public async Task AddAsync(T entity)
        {
            await Context.Set<T>().AddAsync(entity);
        }
        // To Add List of items
        public void InsertList(IEnumerable<T> itemList)
        {
                Context.Set<T>().AddRange(itemList);
                Context.SaveChanges();
        }

        //=========================Update==============================
        // To Update item in table
        public void Update(T entity)
        {

            Context.Set<T>().Update(entity);
            Context.SaveChanges();
        
        }


        // To Update List of items in table
        public void UpdateList(IEnumerable<T> itemList)
       {
            Context.Set<T>().UpdateRange(itemList);
            Context.SaveChanges();
        }
        //=======================Delete===============================
        // To delete item in table
        public void Delete(int? Id)
        {
            var item = Context.Set<T>().Find(Id);

            if (item == null) {
                return;
            }
            Context.Set<T>().Remove(item);
            Context.SaveChanges();    
        }

        public void Delete(T entity)
        {
            Context.Set<T>().Remove(entity);
            Context.SaveChanges();
        }

        // To delete item in table
        public void Delete(int productId, int storeId)
        {
            var item = Context.Set<ProductStock>().FirstOrDefault(p => p.productId == productId && p.storeId == storeId); 

            if (item == null)
            {
                return;
            }
            Context.Set<ProductStock>().Remove(item);
            Context.SaveChanges();
        }


        //To Delet selected items
        public void DeleteList(IEnumerable<T> itemList)
        {
            Context.Set<T>().RemoveRange(itemList);
            Context.SaveChanges();
        }


        public async Task<List<Notification>> GetUnreadForUserAsync(string userId)
        {
            return await Context.notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }


    }
}
