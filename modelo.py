import csv
import os
import requests
import json

class Configuracion:
    def __init__(self):
        self.archivo = 'configuracion.json'
        self.fecha_comunion = "--/--"
        self.fecha_confirmacion = "--/--"
        self.cargar()

    def cargar(self):
        if os.path.exists(self.archivo):
            with open(self.archivo, 'r', encoding='utf-8') as f:
                datos = json.load(f)
                if "fecha_comunion" in datos:
                    self.fecha_comunion = datos["fecha_comunion"]
                if "fecha_confirmacion" in datos:
                    self.fecha_confirmacion = datos["fecha_confirmacion"]
        else:
            self.guardar()

    def guardar(self):
        with open(self.archivo, 'w', encoding='utf-8') as f:
            json.dump({"fecha_comunion": self.fecha_comunion, "fecha_confirmacion": self.fecha_confirmacion}, f)

class BaseDatos:
    def __init__(self):
        self.archivo = 'registros_sigep.csv'
        self.dnis_prin = []
        self.dnis_sec = []
        self.nombres = []
        self.nombres_sec = []
        self.correos = []
        self.generos = []
        self.tipos = []
        self.estados = []
        self.obs_auto = []
        self.obs_adic = []
        self.checks_mem = []
        self.fechas_sac = []
        self.libros = []
        self.folios = []
        self.asientos = []
        self.pagos = []
        self.deudas = []
        self.cargar_datos()

    def crear_cabeceras(self):
        with open(self.archivo, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(['DNI_PRIN', 'DNI_SEC', 'NOMBRES', 'NOMBRES_SEC', 'CORREOS', 'GENERO', 'TIPO', 'ESTADO', 'OBS_AUTO', 'OBS_ADIC', 'CHECKS', 'FECHA_SAC', 'LIBRO', 'FOLIO', 'ASIENTO', 'PAGO', 'DEUDA'])

    def limpiar_arreglos(self):
        self.dnis_prin.clear()
        self.dnis_sec.clear()
        self.nombres.clear()
        self.nombres_sec.clear()
        self.correos.clear()
        self.generos.clear()
        self.tipos.clear()
        self.estados.clear()
        self.obs_auto.clear()
        self.obs_adic.clear()
        self.checks_mem.clear()
        self.fechas_sac.clear()
        self.libros.clear()
        self.folios.clear()
        self.asientos.clear()
        self.pagos.clear()
        self.deudas.clear()

    def cargar_datos(self):
        if not os.path.exists(self.archivo):
            self.crear_cabeceras()
        else:
            self.limpiar_arreglos()
            with open(self.archivo, 'r', encoding='utf-8') as f:
                reader = csv.reader(f)
                next(reader, None)
                for fila in reader:
                    if len(fila) == 17:
                        self.dnis_prin.append(fila[0])
                        self.dnis_sec.append(fila[1])
                        self.nombres.append(fila[2])
                        self.nombres_sec.append(fila[3])
                        self.correos.append(fila[4])
                        self.generos.append(fila[5])
                        self.tipos.append(fila[6])
                        self.estados.append(fila[7])
                        self.obs_auto.append(fila[8])
                        self.obs_adic.append(fila[9])
                        self.checks_mem.append(fila[10])
                        self.fechas_sac.append(fila[11])
                        self.libros.append(fila[12])
                        self.folios.append(fila[13])
                        self.asientos.append(fila[14])
                        self.pagos.append(fila[15])
                        self.deudas.append(fila[16])

    def guardar_datos(self):
        with open(self.archivo, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(['DNI_PRIN', 'DNI_SEC', 'NOMBRES', 'NOMBRES_SEC', 'CORREOS', 'GENERO', 'TIPO', 'ESTADO', 'OBS_AUTO', 'OBS_ADIC', 'CHECKS', 'FECHA_SAC', 'LIBRO', 'FOLIO', 'ASIENTO', 'PAGO', 'DEUDA'])
            length = len(self.dnis_prin)
            for i in range(length):
                writer.writerow([self.dnis_prin[i], self.dnis_sec[i], self.nombres[i], self.nombres_sec[i], self.correos[i], self.generos[i],
                                 self.tipos[i], self.estados[i], self.obs_auto[i], self.obs_adic[i], self.checks_mem[i], self.fechas_sac[i],
                                 self.libros[i], self.folios[i], self.asientos[i], self.pagos[i], self.deudas[i]])

    def registrar(self, d1, d2, nom, nom2, cor, gen, tipo, est, o_auto, o_adic, chks, fec, lib, fol, asi, pag, deu):
        self.dnis_prin.append(d1)
        self.dnis_sec.append(d2)
        self.nombres.append(nom)
        self.nombres_sec.append(nom2)
        self.correos.append(cor)
        self.generos.append(gen)
        self.tipos.append(tipo)
        self.estados.append(est)
        self.obs_auto.append(o_auto)
        self.obs_adic.append(o_adic)
        self.checks_mem.append(chks)
        self.fechas_sac.append(fec)
        self.libros.append(lib)
        self.folios.append(fol)
        self.asientos.append(asi)
        self.pagos.append(pag)
        self.deudas.append(deu)
        self.guardar_datos()

    def actualizar_completo(self, idx, d1, d2, nom, nom2, cor, gen, est, o_auto, o_adic, chks, fec, lib, fol, asi, pag, deu):
        self.dnis_prin[idx] = d1
        self.dnis_sec[idx] = d2
        self.nombres[idx] = nom
        self.nombres_sec[idx] = nom2
        self.correos[idx] = cor
        self.generos[idx] = gen
        self.estados[idx] = est
        self.obs_auto[idx] = o_auto
        self.obs_adic[idx] = o_adic
        self.checks_mem[idx] = chks
        self.fechas_sac[idx] = fec
        self.libros[idx] = lib
        self.folios[idx] = fol
        self.asientos[idx] = asi
        self.pagos[idx] = pag
        self.deudas[idx] = deu
        self.guardar_datos()

    def eliminar(self, idx):
        self.dnis_prin.pop(idx)
        self.dnis_sec.pop(idx)
        self.nombres.pop(idx)
        self.nombres_sec.pop(idx)
        self.correos.pop(idx)
        self.generos.pop(idx)
        self.tipos.pop(idx)
        self.estados.pop(idx)
        self.obs_auto.pop(idx)
        self.obs_adic.pop(idx)
        self.checks_mem.pop(idx)
        self.fechas_sac.pop(idx)
        self.libros.pop(idx)
        self.folios.pop(idx)
        self.asientos.pop(idx)
        self.pagos.pop(idx)
        self.deudas.pop(idx)
        self.guardar_datos()

    def buscar_por_dni(self, dni):
        length = len(self.dnis_prin)
        for i in range(length):
            if self.dnis_prin[i] == dni:
                return i
            elif self.dnis_sec[i] == dni:
                return i
        return -1

    def _swap(self, i, j):
        temp_d1 = self.dnis_prin[i]
        self.dnis_prin[i] = self.dnis_prin[j]
        self.dnis_prin[j] = temp_d1

        temp_d2 = self.dnis_sec[i]
        self.dnis_sec[i] = self.dnis_sec[j]
        self.dnis_sec[j] = temp_d2

        temp_nom = self.nombres[i]
        self.nombres[i] = self.nombres[j]
        self.nombres[j] = temp_nom

        temp_nom2 = self.nombres_sec[i]
        self.nombres_sec[i] = self.nombres_sec[j]
        self.nombres_sec[j] = temp_nom2

        temp_cor = self.correos[i]
        self.correos[i] = self.correos[j]
        self.correos[j] = temp_cor

        temp_gen = self.generos[i]
        self.generos[i] = self.generos[j]
        self.generos[j] = temp_gen

        temp_tip = self.tipos[i]
        self.tipos[i] = self.tipos[j]
        self.tipos[j] = temp_tip

        temp_est = self.estados[i]
        self.estados[i] = self.estados[j]
        self.estados[j] = temp_est

        temp_oba = self.obs_auto[i]
        self.obs_auto[i] = self.obs_auto[j]
        self.obs_auto[j] = temp_oba

        temp_obd = self.obs_adic[i]
        self.obs_adic[i] = self.obs_adic[j]
        self.obs_adic[j] = temp_obd

        temp_chk = self.checks_mem[i]
        self.checks_mem[i] = self.checks_mem[j]
        self.checks_mem[j] = temp_chk

        temp_fec = self.fechas_sac[i]
        self.fechas_sac[i] = self.fechas_sac[j]
        self.fechas_sac[j] = temp_fec

        temp_lib = self.libros[i]
        self.libros[i] = self.libros[j]
        self.libros[j] = temp_lib

        temp_fol = self.folios[i]
        self.folios[i] = self.folios[j]
        self.folios[j] = temp_fol

        temp_asi = self.asientos[i]
        self.asientos[i] = self.asientos[j]
        self.asientos[j] = temp_asi

        temp_pag = self.pagos[i]
        self.pagos[i] = self.pagos[j]
        self.pagos[j] = temp_pag

        temp_deu = self.deudas[i]
        self.deudas[i] = self.deudas[j]
        self.deudas[j] = temp_deu

    def ordenar_bubble_asc(self):
        length = len(self.nombres)
        for i in range(length):
            for j in range(0, length - i - 1):
                if self.nombres[j].lower() > self.nombres[j + 1].lower():
                    self._swap(j, j + 1)
        self.guardar_datos()

    def ordenar_selection_desc(self):
        length = len(self.nombres)
        for i in range(length):
            min_pos = i
            for j in range(i + 1, length):
                if self.nombres[j].lower() > self.nombres[min_pos].lower():
                    min_pos = j
            if min_pos != i:
                self._swap(i, min_pos)
        self.guardar_datos()

class ApiReniec:
    @staticmethod
    def consultar_dni(dni):
        try:
            url = f"https://api.apis.net.pe/v1/dni?numero={dni}"
            r = requests.get(url, timeout=3)
            r.encoding = 'utf-8' 
            if r.status_code == 200:
                d = r.json()
                if 'nombres' in d:
                    return f"{d['nombres']} {d['apellidoPaterno']} {d['apellidoMaterno']}"
            return "" 
        except:
            return ""