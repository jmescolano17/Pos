﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Pos.Server.Class;
using Pos.Shared;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pos.Server.Services
{
    public class UserServices
    {
        private readonly AppDb _constring;
        public IConfiguration Configuration;
        private readonly AppSettings _appSetting;



        public UserServices(AppDb constring, IConfiguration configuration, IOptions<AppSettings> appSettings)
        {
            _constring = constring;
            Configuration = configuration;
            _appSetting = appSettings.Value;
        }

        //View Users
        public async Task<List<user>> User()
        {

            List<user> xuser = new List<user>();
            using (var con = new MySqlConnection(_constring.GetConnection()))
            {
                await con.OpenAsync().ConfigureAwait(false);
                var com = new MySqlCommand("SELECT * FROM pos.tbluser;", con)
                {
                    CommandType = CommandType.Text,
                };
                var rdr = await com.ExecuteReaderAsync().ConfigureAwait(false);
                while (await rdr.ReadAsync().ConfigureAwait(false))
                {
                    xuser.Add(new user
                    {
                        userid = Convert.ToInt32(rdr["userid"]),
                        name = rdr["name"].ToString(),
                        username = rdr["username"].ToString(),
                        accounttype = rdr["accounttype"].ToString(),
                        //token = rdr["token"].ToString(),
                        password = rdr["password"].ToString(),
                    });
                }
            }
            return xuser;
        }


        //User Login
        public async Task<List<user>> Login(string user, string pwd)
        {
            List<user> xuser = new List<user>();
            using (var con = new MySqlConnection(_constring.GetConnection()))
            {
                await con.OpenAsync().ConfigureAwait(false);
                var com = new MySqlCommand("SELECT * FROM tbluser WHERE username = @user AND password = @pass", con)
                {
                    CommandType = CommandType.Text,
                };
                com.Parameters.Clear();
                com.Parameters.AddWithValue("@user", user);
                com.Parameters.AddWithValue("@pass", pwd);
                var rdr = await com.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rdr.ReadAsync().ConfigureAwait(false))
                {
                    xuser.Add(new user
                    {
                        userid = Convert.ToInt32(rdr["userid"]),
                        name = rdr["name"].ToString(),
                        username = rdr["username"].ToString(),
                        accounttype = rdr["accounttype"].ToString(),
                        password = rdr["password"].ToString(),

                    });
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_appSetting.Secret);
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[] {
                    new Claim (ClaimTypes.Name ,user),
                    new Claim(ClaimTypes.Role ,xuser[0].accounttype!),
                    new Claim (JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                    }),

                        Expires = DateTime.UtcNow.AddDays(1),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha512Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    xuser[0].token = tokenHandler.WriteToken(token);
                }
                else
                {
                    xuser.Add(new user
                    {
                        userid = 0,
                        accounttype = null,
                        name = null,
                        username = null,
                        password = null
                    });
                }
            }
            return xuser;
        }
    }
}