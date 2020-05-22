using CandidateProject.EntityModels;
using CandidateProject.ViewModels;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace CandidateProject.Controllers
{
    public class CartonController : Controller
    {
        private CartonContext db = new CartonContext();

        // GET: Carton
        public ActionResult Index()
        {
            var rawCartons = db.Cartons.ToList();
            var cartons = rawCartons.Select(c => new CartonViewModel()
            {
                Id = c.Id,
                CartonNumber = c.CartonNumber,
                CartonEquipmentCount = db.CartonDetails.Where(e => e.CartonId == c.Id).Count()
            });

            return View(cartons);
        }

        // GET: Carton/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // GET: Carton/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carton/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CartonNumber")] Carton carton)
        {
            if (ModelState.IsValid)
            {
                db.Cartons.Add(carton);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(carton);
        }

        // GET: Carton/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CartonNumber")] CartonViewModel cartonViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons.Find(cartonViewModel.Id);
                carton.CartonNumber = cartonViewModel.CartonNumber;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cartonViewModel);
        }

        // GET: Carton/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            var equipments = db.CartonDetails.Where(e => e.CartonId == id).ToList();
            foreach (var equipment in equipments) {
                db.CartonDetails.Remove(equipment);
            }
            db.Cartons.Remove(carton);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult AddEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id
                })
                .SingleOrDefault();

            if (carton == null)
            {
                return HttpNotFound();
            }

            var equipment = db.Equipments
                .Where(e => !db.CartonDetails.Where(cd => cd.CartonId == id).Select(cd => cd.EquipmentId).Contains(e.Id) )
                .Select(e => new EquipmentViewModel()
                {
                    Id = e.Id,
                    ModelType = e.ModelType.TypeName,
                    SerialNumber = e.SerialNumber
                })
                .ToList();
            
            carton.Equipment = equipment;
            return View(carton);
        }

        public ActionResult AddEquipmentToCarton([Bind(Include = "CartonId,EquipmentId")] AddEquipmentViewModel addEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons
                    .Include(c => c.CartonDetails)
                    .Where(c => c.Id == addEquipmentViewModel.CartonId)
                    .SingleOrDefault();
                if (carton == null)
                {
                    return HttpNotFound();
                }

                //validate if there are more than 10 equipments in a carton
                var equipmentsCount = db.CartonDetails.Where(e => e.CartonId == addEquipmentViewModel.CartonId).Count();
                if (equipmentsCount >= 10) {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Carton reaches maximum capicity, can't add more equipment!!!");
                }

                var equipment = db.Equipments
                    .Where(e => e.Id == addEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();
                if (equipment == null)
                {
                    return HttpNotFound();
                }

                //check if one equipment is already in this or other cartons
                var checkIfInCarton = db.CartonDetails.Where(e => e.EquipmentId == addEquipmentViewModel.EquipmentId).SingleOrDefault();
                if (checkIfInCarton != null)
                {
                    if (checkIfInCarton.CartonId == addEquipmentViewModel.CartonId)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Equipment already exists in current carton!!!");
                    }
                    else
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Equipment already exists in carton " + checkIfInCarton.CartonId + "!!!");
                    }
                }
                else {
                    var detail = new CartonDetail()
                    {
                        Carton = carton,
                        Equipment = equipment
                    };
                    carton.CartonDetails.Add(detail);
                    db.SaveChanges();
                }              
            }
            return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
        }

        public ActionResult ViewCartonEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id,
                    Equipment = c.CartonDetails
                        .Select(cd => new EquipmentViewModel()
                        {
                            Id = cd.EquipmentId,
                            ModelType = cd.Equipment.ModelType.TypeName,
                            SerialNumber = cd.Equipment.SerialNumber
                        })
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        public ActionResult RemoveEquipmentOnCarton([Bind(Include = "CartonId,EquipmentId")] RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                var equipment = db.CartonDetails.
                    Where(e => e.CartonId == removeEquipmentViewModel.CartonId && e.EquipmentId == removeEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();
                if (equipment != null)
                {
                    db.CartonDetails.Remove(equipment);
                    db.SaveChanges();
                }
                else {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,"Equipment is not found!!!");
                }
            }
            return RedirectToAction("ViewCartonEquipment", new { id = removeEquipmentViewModel.CartonId });
        }

        public ActionResult RemoveAllEquipmentOnCarton(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        //Add a view to confirm delete all equipment from a carton 
        [HttpPost, ActionName("RemoveAllEquipmentOnCarton")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveAllConfirmed(int id) {
            var equipments = db.CartonDetails.Where(e => e.CartonId == id).ToList();
            foreach (var equipment in equipments)
            {
                db.CartonDetails.Remove(equipment);
            }
            db.SaveChanges();
            return RedirectToAction("ViewCartonEquipment", new { id = id });
        }
    }
}
