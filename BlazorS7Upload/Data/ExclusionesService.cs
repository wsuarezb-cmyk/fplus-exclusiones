using BlazorS7Upload.Interfaces;
using BlazorS7Upload.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using System.Data;
using Dapper;
using Npgsql;

namespace BlazorS7Upload.Data
{
    public class ExclusionesService : IExclusionesService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public ExclusionesService(IConfiguration configuration)
        {

            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("db_contenido_");

        }
        public async Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais)
        {
            try
            {
                // Almacenar los resultados en una lista de objetos de la clase exclusiones
                List<ExclusionesModel> exclusiones = new List<ExclusionesModel>();

                if (pais == "TEST") pais = "CO";

                var total = parameterValues.Count;

                var division = 500;

                var vueltas = Math.Ceiling((decimal)total / division);

                for( var i = 0; i < vueltas; i++)
                {
                    var parameter = parameterValues.Skip(i * 500).Take(500).ToList();
                    // Establece tus credenciales de autenticación
                    GoogleCredential credential = GoogleCredential.FromFile(_configuration["BigQuery:CredentialsPath"]);

                    // Crea un cliente de BigQuery utilizando las credenciales
                    BigQueryClient client = BigQueryClient.Create(projectId: _configuration["BigQuery:ProjectId"], credential: credential);

                    // Crea el parámetro con el tipo y los valores correspondientes
                    var queryParameters = new BigQueryParameter[]
                    {
                        new BigQueryParameter("parametro", BigQueryDbType.Array, parameter),
                        new BigQueryParameter("pais", BigQueryDbType.String, pais)
                    };

                    // Define tu consulta
                    string query = @"
                        WITH t_orders AS (
                          SELECT 1 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_cl_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_cl`
                        WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                        UNION ALL
                        SELECT 2 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_pe_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_pe`
                        WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                        UNION ALL
                        SELECT 3 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_co_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_co`
                        WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                        )

                        SELECT DISTINCT CAST(CURRENT_DATE('America/Bogota') AS STRING) as date,
                        c.short_code as sellerId,
                        a.delivery_order_number as deliveryOrderNumber,
                        '' AS itemId,
                        '' AS sku_falabella,
                        '' AS responsable,
                        '' AS motivo,
                        '' AS kpi_name,
                        IF(a.fk_operator = 1, 'Chile',
                        IF(a.fk_operator = 2, 'Perú',
                        IF(a.fk_operator = 3, 'Colombia',
                        ''))) AS pais
                        FROM t_orders AS a
                        LEFT JOIN `txd-fal-corp-3p-acc-wbx.acc_fal_reg_catalog_sellin.svw_tc_sc_bi_bigdata_dtl_fcom_prd_trf_corp_drmb_sllm_svw_gsc_production_seller` AS c ON c.short_code = a.seller_id
                        WHERE a.delivery_order_number IN UNNEST(@parametro)
                        AND IF(a.fk_operator = 1, 'CL',
                        IF(a.fk_operator = 2, 'PE',
                        IF(a.fk_operator = 3, 'CO',
                        ''))) = @pais
                    ";

                    // Ejecuta la consulta
                    BigQueryJob job = await client.CreateQueryJobAsync(query, queryParameters);
                    BigQueryResults results = await job.GetQueryResultsAsync();

                    

                    foreach (BigQueryRow fila in results)
                    {
                        ExclusionesModel exclusion = new ExclusionesModel
                        {
                            date = fila["date"].ToString(),
                            sellerId = fila["sellerId"].ToString(),
                            deliveryOrderNumber = fila["deliveryOrderNumber"].ToString(),
                            itemId = fila["itemId"].ToString(),
                            sku_falabella = fila["sku_falabella"].ToString(),
                            responsable = fila["responsable"].ToString(),
                            motivo = fila["motivo"].ToString(),
                            kpi_name = fila["kpi_name"].ToString(),
                            pais = fila["pais"].ToString()
                        };

                        exclusiones.Add(exclusion);
                    }
                }


                return exclusiones;

            }
            catch (Exception)
            {
                return null;
            }

        }

        public async Task<List<ExclusionesModel>> ConsultarOrdenesBigQ(List<string> parameterValues, string pais, SemaphoreSlim semaphore)
        {
            try
            {
                // Almacenar los resultados en una lista de objetos de la clase exclusiones
                List<ExclusionesModel> exclusiones = new List<ExclusionesModel>();

                if (pais == "TEST") pais = "CO";

                // Establece tus credenciales de autenticación
                GoogleCredential credential = GoogleCredential.FromFile(_configuration["BigQuery:CredentialsPath"]);

                // Crea un cliente de BigQuery utilizando las credenciales
                BigQueryClient client = BigQueryClient.Create(projectId: _configuration["BigQuery:ProjectId"], credential: credential);

                // Crea el parámetro con el tipo y los valores correspondientes
                var queryParameters = new BigQueryParameter[]
                {
                    new BigQueryParameter("parametro", BigQueryDbType.Array, parameterValues),
                    new BigQueryParameter("pais", BigQueryDbType.String, pais)
                };

                // Define tu consulta
                string query = @"
                WITH t_orders AS (
                  SELECT 1 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_cl_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_cl`
                WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                UNION ALL
                SELECT 2 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_pe_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_pe`
                WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                UNION ALL
                SELECT 3 as fk_operator, seller_id, delivery_order_number FROM `txd-fal-corp-3p-acc-wbx.acc_fal_co_tran_sellin.svw_bi_fcom_drmb_sharing_hub_sbx_svw_fcm_corp_seller_order_slor_prd_svw_vw_seller_order_co`
                WHERE last_modified_at >= DATE_SUB(CURRENT_DATE('America/Bogota'), INTERVAL 2 YEAR)
                )

                SELECT DISTINCT CAST(CURRENT_DATE('America/Bogota') AS STRING) as date,
                c.short_code as sellerId,
                a.delivery_order_number as deliveryOrderNumber,
                '' AS itemId,
                '' AS sku_falabella,
                '' AS responsable,
                '' AS motivo,
                '' AS kpi_name,
                IF(a.fk_operator = 1, 'Chile',
                IF(a.fk_operator = 2, 'Peru',
                IF(a.fk_operator = 3, 'Colombia',
                ''))) AS pais,
                STRING_AGG(DISTINCT IF(d.deliveryOrderNumber IS NULL, '', CONCAT('Excluido', '=', d.kpi_name))) AS Excluido
                FROM t_orders AS a
                LEFT JOIN `txd-fal-corp-3p-acc-wbx.acc_fal_reg_catalog_sellin.svw_tc_sc_bi_bigdata_dtl_fcom_prd_trf_corp_drmb_sllm_svw_gsc_production_seller` AS c ON c.short_code = a.seller_id
                LEFT JOIN `bi-fcom-drmb-sell-in-sbx.sandbox_avicunac.t_log_sx_gsc_exclusiones_co` AS d ON d.deliveryOrderNumber = a.delivery_order_number AND a.fk_operator = 3
                WHERE a.delivery_order_number IN UNNEST(@parametro)
                AND IF(a.fk_operator = 1, 'CL',
                IF(a.fk_operator = 2, 'PE',
                IF(a.fk_operator = 3, 'CO',
                ''))) = @pais
                GROUP BY ALL
                ";

                // Ejecuta la consulta
                BigQueryJob job = await client.CreateQueryJobAsync(query, queryParameters);
                BigQueryResults results = await job.GetQueryResultsAsync();



                foreach (BigQueryRow fila in results)
                {
                    ExclusionesModel exclusion = new ExclusionesModel
                    {
                        date = fila["date"].ToString(),
                        sellerId = fila["sellerId"].ToString(),
                        deliveryOrderNumber = fila["deliveryOrderNumber"].ToString(),
                        itemId = fila["itemId"].ToString(),
                        sku_falabella = fila["sku_falabella"].ToString(),
                        responsable = fila["responsable"].ToString(),
                        motivo = fila["motivo"].ToString(),
                        kpi_name = fila["kpi_name"].ToString(),
                        pais = fila["pais"].ToString(),
                        Excluido = fila["Excluido"].ToString(),
                    };

                    exclusiones.Add(exclusion);
                }
                
                return exclusiones;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR ConsultarOrdenesBigQ: {ex.Message}");
                return null;
            }
            finally 
            {
                semaphore.Release();
            }

        }

        public async Task<List<ExclusionesModel>> GetListAsync(List<string> parameterValues)
        {
            IEnumerable<ExclusionesModel> results;
            var deliveryOrderNumber = string.Concat("'", string.Join("','", parameterValues), "'");

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var parametros = new { deliveryOrderNumber = deliveryOrderNumber };
                string query = $"SELECT * FROM exclusiones.sx_gsc_exclusiones_co WHERE \"deliveryOrderNumber\" IN (@deliveryOrderNumber)";

                results = await conn.QueryAsync<ExclusionesModel>(query, parametros);
            }

            return results.ToList();
        }

        public async Task<string> SaveChangesAsync(IList<ExclusionesModel> _exclusionesModel, string pais)
        {
            var sql = string.Empty;
            using var conn = new NpgsqlConnection(_connectionString);
            if(pais == "CO")
            {
                sql = "INSERT INTO exclusiones.sx_gsc_exclusiones_co (date, \"sellerId\", \"deliveryOrderNumber\", \"itemId\", sku_falabella, responsable, motivo, kpi_name, pais, comentario, tipo_exclusion) VALUES (@date, @sellerId, @deliveryOrderNumber, @itemId, @sku_falabella, @responsable, @motivo, @kpi_name, @pais, @comentario, @tipo_exclusion)";
            }
            if (pais == "PE")
            {
                sql = "INSERT INTO exclusiones.sx_gsc_exclusiones_pe (date, \"sellerId\", \"deliveryOrderNumber\", \"itemId\", sku_falabella, responsable, motivo, kpi_name, pais, comentario, tipo_exclusion) VALUES (@date, @sellerId, @deliveryOrderNumber, @itemId, @sku_falabella, @responsable, @motivo, @kpi_name, @pais, @comentario, @tipo_exclusion)";
            }
            if (pais == "CL")
            {
                sql = "INSERT INTO exclusiones.sx_gsc_exclusiones_cl (date, \"sellerId\", \"deliveryOrderNumber\", \"itemId\", sku_falabella, responsable, motivo, kpi_name, pais, comentario, tipo_exclusion) VALUES (@date, @sellerId, @deliveryOrderNumber, @itemId, @sku_falabella, @responsable, @motivo, @kpi_name, @pais, @comentario, @tipo_exclusion)";
            }


            var rowsAffected = await conn.ExecuteAsync(sql, _exclusionesModel);

            return $"Exclusiones efectuadas satisfactoriamente: {rowsAffected}";
        }

        #region "Kpi de exclusiones"
        public async Task<List<ExclusionesKpiModel>> GetAllKpiAsync()
        {
            IEnumerable<ExclusionesKpiModel> results;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = $"SELECT * FROM exclusiones.sx_gsc_exclusiones_kpi WHERE status <> 'deleted'";

                results = await conn.QueryAsync<ExclusionesKpiModel>(query);
            }

            return results.ToList();
        }

        public async Task<string> InsertKpiAsync(ExclusionesKpiModel _model)
        {

            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { name = _model.name };
            var sql = @"INSERT INTO exclusiones.sx_gsc_exclusiones_kpi (name, status) VALUES(@name, 'active')";

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Kpi insertado satisfactoriamente: {rowsAffected}";
        }

        public async Task<string> UpdateKpiAsync(ExclusionesKpiModel _model)
        {

            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { id = _model.id, name = _model.name, status = _model.status };
            var sql = @"UPDATE exclusiones.sx_gsc_exclusiones_kpi
                        SET name = @name,
                        status = @status
                        WHERE id = @id".Trim();

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Kpi actualizado satisfactoriamente: {rowsAffected}";
        }

        public async Task<string> DeleteKpiAsync(int id)
        {

            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { id = id };
            var sql = @"UPDATE exclusiones.sx_gsc_exclusiones_kpi
                        SET status = 'deleted'
                        WHERE id = @id".Trim();

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Kpi eliminado satisfactoriamente: {rowsAffected}";
        }

        #endregion

        #region "Motivo de exclusiones"

        public async Task<List<ExclusionesMotivoModel>> GetAllMotivoAsync()
        {
            IEnumerable<ExclusionesMotivoModel> results;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = $@"SELECT a.*,b.name as kpi_name 
FROM exclusiones.sx_gsc_exclusiones_motivo AS a
LEFT JOIN exclusiones.sx_gsc_exclusiones_kpi AS b ON b.id = a.fk_kpi
WHERE a.status <> 'deleted'";

                results = await conn.QueryAsync<ExclusionesMotivoModel>(query);
            }

            return results.ToList();
        }

        public async Task<string> InsertMotivoAsync(ExclusionesMotivoModel _model)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { fk_kpi = _model.fk_kpi, name = _model.name };
            var sql = @"INSERT INTO exclusiones.sx_gsc_exclusiones_motivo (fk_kpi, name, status) VALUES(@fk_kpi, @name, 'active')";

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Motivo insertado satisfactoriamente: {rowsAffected}";
        }

        public async Task<string> UpdateMotivoAsync(ExclusionesMotivoModel _model)
        {

            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { id = _model.id, name = _model.name, status = _model.status };
            var sql = @"UPDATE exclusiones.sx_gsc_exclusiones_motivo
                        SET name = @name,
                        status = @status
                        WHERE id = @id".Trim();

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Motivo actualizado satisfactoriamente: {rowsAffected}";
        }

        public async Task<string> DeleteMotivoAsync(int id)
        {

            using var conn = new NpgsqlConnection(_connectionString);
            var parametros = new { id = id };
            var sql = @"UPDATE exclusiones.sx_gsc_exclusiones_motivo
                        SET status = 'deleted'
                        WHERE id = @id".Trim();

            var rowsAffected = await conn.ExecuteAsync(sql, parametros);

            return $"Motivo eliminado satisfactoriamente: {rowsAffected}";
        }

        #endregion

    }
}
