using WalkyDoggy.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace WalkyDoggy.Data.Repositories
{
   

    public interface IRepositorioEntidadBase<T>  where T : class, IEntityBase, new()
    {
        IQueryable<T> TodosConIncluidos(params Expression<Func<T, object>>[] propiedadesIncluidas);
        IQueryable<T> All { get; }
        IQueryable<T> ObtenerTodos();
        T ObtenerUno(Int64 id);
        IQueryable<T> BuscarPor(Expression<Func<T, bool>> predicado);
        void Agregar(T entidad);
        void Eliminar(T entidad);
        void Editar(T entidad);
    }
}
