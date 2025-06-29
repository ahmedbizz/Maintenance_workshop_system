using System.Collections.Generic;
using WorkShop.Models;

namespace WorkShop.Repository.Base
{
    
        public interface IRepository<T>where T : class
{


        T FindById(int? Id);
        T FindById(string? Id);

        ProductStock FindByKeys(int? productId, int storeId);
         
        IEnumerable<T> FindAll();
        IEnumerable<T> FindAll(params string[] agers);

        void Delete(int? Id);
        void Delete(T entity);
        void Delete(int productId, int storeId);

        void Insert(T? entity);

        Task AddAsync(T entity);

        void Update(T? entity);



        void DeleteList(IEnumerable<T> ItemList);

        void InsertList(IEnumerable<T>? ItemList);

        void UpdateList(IEnumerable<T>? ItemList);


        Task<List<Notification>> GetUnreadForUserAsync(string userId);



    }


}