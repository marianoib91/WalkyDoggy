using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Entities;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace WalkyDoggy.Data.Repositories
{
    public class RepositorioEntidadBase<T> : IRepositorioEntidadBase<T>
            where T : class, IEntityBase, new()
    {

        private WalkyDoggyContext contextoDatos;

        #region Propiedades
        protected IFabricaDb Fabrica
        {
            get;
            private set;
        }

        protected WalkyDoggyContext Contexto
        {
            get { return contextoDatos ?? (contextoDatos = Fabrica.Iniciar()); }
        }
        public RepositorioEntidadBase(IFabricaDb fabricaDb)
        {
            Fabrica = fabricaDb;
        }
        #endregion
        public virtual IQueryable<T> ObtenerTodos()
        {
            return Contexto.Set<T>();
        }
        public virtual IQueryable<T> All
        {
            get
            {
                return ObtenerTodos();
            }
        }
        public virtual IQueryable<T> TodosConIncluidos(params Expression<Func<T, object>>[] propiedadesIncluidas)
        {
            IQueryable<T> consulta = Contexto.Set<T>();
            foreach (var propiedadIncluida in propiedadesIncluidas)
            {
                consulta = consulta.Include(propiedadIncluida);
            }
            return consulta;
        }
        
        public T ObtenerUno(Int64 id)
        {
            return ObtenerTodos().FirstOrDefault(x => x.Id == id);
        }
        public virtual IQueryable<T> BuscarPor(Expression<Func<T, bool>> predicado)
        {
            return Contexto.Set<T>().Where(predicado);
        }

        public virtual void Agregar(T entidad)
        {
            DbEntityEntry entradaDeEntidad = Contexto.Entry<T>(entidad);
            Contexto.Set<T>().Add(entidad);
        }
        public virtual void Editar(T entidad)
        {
            DbEntityEntry entradaDeEntidad = Contexto.Entry<T>(entidad);
            entradaDeEntidad.State = EntityState.Modified;
        }
        public virtual void Eliminar(T entidad)
        {
            DbEntityEntry entradaDeEntidad = Contexto.Entry<T>(entidad);
            entradaDeEntidad.State = EntityState.Deleted;
        }
    }
}
