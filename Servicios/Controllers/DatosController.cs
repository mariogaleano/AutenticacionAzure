using Servicios.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Servicios.Controllers
{
    [EnableCors("*","*","*")]
    [Authorize]
    public class DatosController : ApiController
    {
        public List<Persona> GetPersonas()
        {
            List<Persona> lista = new List<Persona>();
            for (int i = 10; i < 60; i++)
            {
                lista.Add(new Persona
                {
                    Email = string.Format("persona{0}@email.com", i),
                    Id = i * 100,
                    FechaNacimiento = DateTime.Now.AddYears(-i * 2).AddMonths(i * 3),
                    Nombre = string.Format("Persona {0}",i)
                });
            }

            return lista;
        }
    }
}
