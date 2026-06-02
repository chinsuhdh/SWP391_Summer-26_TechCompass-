using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Repository_TechCompass.Repositories
{
    public class MentorRepository : IMentorRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public MentorRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public List<Mentor> GetAllMentors()
        {
            // Join với bảng User để lấy Email và IsActive
            return _context.Mentors.Include(m => m.User).ToList();
        }

        public Mentor? GetMentorById(Guid mentorId)
        {
            return _context.Mentors.Include(m => m.User).FirstOrDefault(m => m.MentorId == mentorId);
        }

        public Mentor? GetMentorByUserId(Guid userId)
        {
            return _context.Mentors.Include(m => m.User).FirstOrDefault(m => m.UserId == userId);
        }

        public void AddMentor(Mentor mentor) => _context.Mentors.Add(mentor);
        public void UpdateMentor(Mentor mentor) => _context.Mentors.Update(mentor);
        public void DeleteMentor(Mentor mentor) => _context.Mentors.Remove(mentor);
        public void SaveChanges() => _context.SaveChanges();
    }
}