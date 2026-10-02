using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities.Entities
{
   public abstract class EntityBase:IEntityBase
    {
        public Int64 Id { get; set; }

        public DateTime UpdatedDateTime { get; set; }

        public Int64 UpdatedBy { get; set; }

        public Int32 Status { get; set; }

        // Usar deepCopy en true cuando los items del detalle existen mas alla del item principal. Ejemplo: usuarios con roles.
        // Usar deepCopy en false cuando los items se crean junto con el item principal. Ejemplo: pedido y sus renglones.
        // Revisar el metodo UpdateEntityColllections de RepositoryBase para actualizar los items del detalle.
        public void CopiarA(EntityBase entidadBase, Boolean copiaProfunda)
        {
            foreach (PropertyInfo propiedad in this.GetType().GetProperties())
            {
                if (copiaProfunda ||
                    (!copiaProfunda && !EsIEnumerable(propiedad)))
                {
                    object valorACopiar = propiedad.GetValue(this, null);

                    MethodInfo metodoAsignador = propiedad.GetSetMethod();

                    if (metodoAsignador != null)
                    {
                        propiedad.SetValue(entidadBase, valorACopiar, null);
                    }
                }
            }
        }

        private Boolean EsIEnumerable(PropertyInfo propiedad)
        {
            return (propiedad.PropertyType != typeof(string) &&
                   propiedad.PropertyType.GetInterface(typeof(IEnumerable).Name) != null &&
                   propiedad.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null);
        }

        public EntityBase()
        {
            this.UpdatedDateTime = DateTime.Now;
            this.Status = 1;
        }
    }
}
