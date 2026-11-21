import { Elysia, t } from "elysia";
import { prisma } from "./lib/prisma";

// Los datos que se piden para un estudiante
const datosEstudiante = t.Object({
  matricula: t.String(),
  nombre: t.String(),
  apellidos: t.String(),
  email: t.String(),
  telefono: t.Optional(t.String()),
  fechaNacimiento: t.String(), // formato: "2015-05-20"
  grado: t.Integer(),
  grupo: t.String(),
  activo: t.Optional(t.Boolean()),
});

const app = new Elysia()
  .get("/", () => ({ mensaje: "API de Gestión Escolar funcionando " }))

  // VER todos los estudiantes
  .get("/estudiantes", () =>
    prisma.estudiante.findMany({ orderBy: { id: "asc" } })
  )

  // VER un estudiante
  .get("/estudiantes/:id", async ({ params, set }) => {
    const id = Number(params.id);
    if (Number.isNaN(id)) {
      set.status = 400;
      return { error: "El id debe ser un número" };
    }

    const estudiante = await prisma.estudiante.findUnique({ where: { id } });
    if (!estudiante) {
      set.status = 404;
      return { error: "Estudiante no encontrado" };
    }
    return estudiante;
  })

  // CREAR un estudiante
  .post(
    "/estudiantes",
    ({ body }) =>
      prisma.estudiante.create({
        data: { ...body, fechaNacimiento: new Date(body.fechaNacimiento) },
      }),
    { body: datosEstudiante }
  )

  // EDITAR un estudiante
  .put(
    "/estudiantes/:id",
    async ({ params, body, set }) => {
      const id = Number(params.id);
      if (Number.isNaN(id)) {
        set.status = 400;
        return { error: "El id debe ser un número" };
      }

      const existe = await prisma.estudiante.findUnique({ where: { id } });
      if (!existe) {
        set.status = 404;
        return { error: "Estudiante no encontrado" };
      }

      const { fechaNacimiento, ...resto } = body;
      return prisma.estudiante.update({
        where: { id },
        data: {
          ...resto,
          ...(fechaNacimiento
            ? { fechaNacimiento: new Date(fechaNacimiento) }
            : {}),
        },
      });
    },
    { body: t.Partial(datosEstudiante) }
  )

  // BORRAR un estudiante
  .delete("/estudiantes/:id", async ({ params, set }) => {
    const id = Number(params.id);
    if (Number.isNaN(id)) {
      set.status = 400;
      return { error: "El id debe ser un número" };
    }

    const existe = await prisma.estudiante.findUnique({ where: { id } });
    if (!existe) {
      set.status = 404;
      return { error: "Estudiante no encontrado" };
    }

    return prisma.estudiante.delete({ where: { id } });
  })

  .listen(3000);

console.log(` API lista en http://localhost:${app.server?.port}`);