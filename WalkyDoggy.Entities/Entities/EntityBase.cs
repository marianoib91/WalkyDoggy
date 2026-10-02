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
        public void CopyTo(EntityBase entityBase, Boolean deepCopy)
        {
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                if (deepCopy ||
                    (!deepCopy && !IsIEnumerable(pi)))
                {
                    object valueToCopy = pi.GetValue(this, null);

                    MethodInfo setMethod = pi.GetSetMethod();

                    if (setMethod != null)
                    {
                        pi.SetValue(entityBase, valueToCopy, null);
                    }
                }
            }
        }

        private Boolean IsIEnumerable(PropertyInfo pi)
        {
            return (pi.PropertyType != typeof(string) &&
                   pi.PropertyType.GetInterface(typeof(IEnumerable).Name) != null &&
                   pi.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null);
        }

        public EntityBase()
        {
            this.UpdatedDateTime = DateTime.Now;
            this.Status = 1;
        }
    }
}
