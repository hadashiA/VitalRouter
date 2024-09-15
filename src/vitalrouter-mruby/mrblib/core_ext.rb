class Numeric
  def secs
    VitalRouter::TimeDuration.new self
  end
  alias :sec :secs

  def millisecs
    VitalRouter::TimeDuration.new self * 0.001
  end
  alias :millisec :millisecs

  def frames
    VitalRouter::FrameDuration.new self.to_i
  end
  alias :frame :frames
end
