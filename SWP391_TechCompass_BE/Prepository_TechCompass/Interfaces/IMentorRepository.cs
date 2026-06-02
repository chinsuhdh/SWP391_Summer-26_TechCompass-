using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Interfaces
{
    public interface IMentorRepository
    {
        List<Mentor> GetAllMentors();
        Mentor? GetMentorById(Guid mentorId);
        Mentor? GetMentorByUserId(Guid userId);
        void AddMentor(Mentor mentor);
        void UpdateMentor(Mentor mentor);
        void DeleteMentor(Mentor mentor);
        void SaveChanges();
    }
}